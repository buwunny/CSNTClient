using System.Net.WebSockets;
using System.Text.Json;
using System.Text;

using MessagePack;

namespace NTClient
{
  public class Client
  {
    private readonly int port = 5810; // Default NT4 port
    private readonly string ip = "localhost"; // Default NT4 address
    private readonly string name = "NT4";
    private readonly ClientWebSocket client;

    private readonly int timeout = 3;
    private long lastPing = DateTimeOffset.UtcNow.ToUnixTimeSeconds();

    private Topic[] clientTopics = [];
    private Topic[] serverTopics = [];
    // {serverTopicID : Topic}
    
    private Subscriber[] subscribers = [];

    public Client(string ip, string clientName)
    {
      this.ip = ip;
      name = clientName;
      client = new ClientWebSocket();
    }

    public async void Connect()
    {
      Log($"Connecting to {ip}...");
      client.Options.AddSubProtocol("networktables.first.wpi.edu"); //v4.1.networktables.first.wpi.edu
      var address = $"ws://{ip}:{port}/nt/{name}";
      try{
        client.ConnectAsync(new Uri(address), default).Wait();
      }
      catch (Exception e){
        Log($"Failed to connect to server: {e.Message}");
        return;
      }
      Log("Connected to server.");
      // AlivenessCheck();
      // await WebsocketListener();
      await Task.WhenAll(AlivenessCheck(), WebsocketListener());
    }

    public void Disconnect()
    {
      if (client == null) return;
      client.CloseAsync(WebSocketCloseStatus.NormalClosure, "Client disconnected", default).Wait();
      client.Dispose();
    }

    public void Reconnect()
    {

    }

    public bool IsConnected => client?.State == WebSocketState.Open;

    public void UpdateTopic(string topic, object value)
    {
      Topic? clientTopic = clientTopics.FirstOrDefault(t => t.Name == topic);
      if (clientTopic == null)
      {
        Log($"Topic with name \"{topic}\" not found.");
        return;
      }
      uint publisherId = (uint)clientTopic.PubUid;
      long timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() * 1000;
      uint dataType = (uint)TypeLookup(clientTopic.Type);
      var dataValue = value;
      var txData = new object[] { publisherId, timestamp, dataType, value };

      byte[] data = MessagePackSerializer.Serialize(txData);
      SendBinary(data);
    }

    public object? GetTopicValue(string topic)
    {
      Topic? serverTopic = serverTopics.FirstOrDefault(t => t.Name == topic);
      if (serverTopic == null)
      {
        Log($"Topic with name \"{topic}\" not found.");
        return null;
      }

      return serverTopic.Value;
    }

    private async Task WebsocketListener()
    {
      var buffer = new ArraySegment<byte>(new byte[1024]);
      while (IsConnected)
      {
        WebSocketReceiveResult result = await client.ReceiveAsync(buffer, default);
        if (result?.MessageType == WebSocketMessageType.Close)
        {
          Log($"Connection closed by server [{ip}].");
          break;
        }
        else if (buffer.Array != null)
        {
          if (result?.MessageType == WebSocketMessageType.Text)
          {
            HandleJson(buffer.Array, result.Count);
          }
          else if (result?.MessageType == WebSocketMessageType.Binary)
          {
            HandleBinary(buffer.Array);
          }
        }
      }
    }

    private async Task AlivenessCheck()
    {
      while (IsConnected)
      {
        if (DateTimeOffset.UtcNow.ToUnixTimeSeconds() - lastPing > timeout)
        {
          Log("Connection timed out.");
          Disconnect();
          break;
        }
        await Task.Delay(1000);
        SendTimestamp();
        lastPing = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
      }
    }

    // CLIENT -> SERVER

    // Publish Messages (Client to Server)
    public void Publish(string type, string topic)
    {
      Topic newTopic = new()
      {
        Name = topic,
        PubUid = GetNewUID(),
        Type = type
      };

      SendJson("publish", newTopic.GetPublishObject());
      clientTopics = clientTopics.Append(newTopic).ToArray();
    }
    // TODO: start sending values via msgpack after publish

    public void Unpublish(string topic)
    {
      Topic? newTopic = clientTopics.FirstOrDefault(t => t.Name == topic);
      if (newTopic == null)
      {
        Log($"Topic with name \"{topic}\" not found.");
        return;
      };
      SendJson("unpublish", newTopic.GetUnpublishObject());
    }
    // TODO: stop sending values via msgpack after unpublish

    public void SetProperties(string topic, Dictionary<string, object> properties)
    {
      var clientTopic = clientTopics.FirstOrDefault(t => t.Name == topic);
      var serverTopic = serverTopics.FirstOrDefault(t => t.Name == topic);
      if (clientTopic != null)
      {
        clientTopic.Properties = properties;
      }
      if (serverTopic != null)
      {
        serverTopic.Properties = properties;
      }
      SendJson("setproperties", new Dictionary<string, object> { { "name", topic }, { "update", properties } });
    }
    
    public void Subscribe(string[] topics)
    {
      if (client == null) return;
      Subscriber sub = new Subscriber(topics, GetNewUID(), new SubscriptionOptions());
      subscribers.Append(sub);
      SendJson("subscribe", sub.GetSubscribeObject());
      Log("Subscribed to topic(s): \"" + string.Join(", ", topics) + "\"");
    }

    public void Unsubscribe(int uid)
    {
      if (client == null) return;
      Subscriber? sub = subscribers.FirstOrDefault(s => s.Uid == uid);
      if (sub == null)
      {
        Log($"Subscriber with UID {uid} not found.");
        return;
      };
      SendJson("unsubscribe", sub.GetUnsubscribeObject());
      Log("Unsubscribed from topic: \"" + sub.Topics[0] + "\"");
      subscribers = subscribers.Where(s => s.Uid != uid).ToArray();
    }

    public void SendTimestamp()
    {
      long time = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() * 1000;
      var txData = new object[] { -1, 0, 1, time };
      byte[] encodedData = MessagePackSerializer.Serialize(txData);
      SendBinary(encodedData);
      Log($"Sent timestamp: {time}");
    }

    public long timeOffset;
    public void HandleTimestamp(long timestamp)
    {
      long time = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() * 1000;
      timeOffset = time - timestamp;
    }

    private void SendJson(string method, object parameters)
    {
      if (client == null || !IsConnected) return;
      
      List<object> message = new List<object>
      {
        new Dictionary<string, object>
        {
          { "method", method },
          { "params", parameters }
        }
      };
      string json = JsonSerializer.Serialize(message);
      byte[] bytes = Encoding.UTF8.GetBytes(json);
      try
      {
        Log($"Sending JSON: {json}");
        client.SendAsync(new ArraySegment<byte>(bytes), WebSocketMessageType.Text, true, default).Wait();
      }
      catch (Exception ex)
      {
        Log($"ERROR: SendJSON failed: {ex.Message}");
      }
    }

    private void SendBinary(byte[] data)
    {
      if (client == null || !IsConnected) return;
      client.SendAsync(new ArraySegment<byte>(data), WebSocketMessageType.Binary, true, default).Wait();
    }

    // SERVER -> CLIENT
    private void HandleAnnounce(JsonElement parameters)
    {
      try {
        string? name = parameters.GetProperty("name").GetString();
        int id = parameters.GetProperty("id").GetInt32();
        string? type = parameters.GetProperty("type").GetString();
        // int pubuid = parameters.GetProperty("pubuid").GetInt32();

        JsonElement properties = parameters.GetProperty("properties");
        Dictionary<string, object>? propertiesDict = JsonSerializer.Deserialize<Dictionary<string, object>>(properties);
        
        //public Topic(string name, int uid, string type, Dictionary<string, object> properties)
        if (type == null || name == null || propertiesDict == null) { return; }
        Topic newTopic = new()
        {
          Id = id,
          Name = name,
          //PubUid = pubuid,
          Type = type,
          Properties = propertiesDict
        };
        serverTopics = serverTopics.Append(newTopic).ToArray();

        Log($"serverTopics: {serverTopics.Length}");
      }
      catch (Exception e)
      {
        Log($"ERROR: Failed to parse announce: {e.Message}");
        return;
      }
    }

    private void HandleUnannounce(JsonElement parameters) 
    {
      string? name = parameters.GetProperty("name").GetString();
      JsonElement id = parameters.GetProperty("id");
      Topic? topic = serverTopics.FirstOrDefault(t => t.Name == name);
      if (topic != null)
      {
        serverTopics = serverTopics.Where(t => t.Name != name).ToArray();
      }
      else
      {
        Log($"Topic with name \"{name}\" not found.");
      }
    }

    private void HandleProperties(JsonElement parameters)
    {
      try
      {
        string? name = parameters.GetProperty("name").GetString();
        Topic? topic = serverTopics.FirstOrDefault(t => t.Name == name);
        
        JsonElement update = parameters.GetProperty("update");
        Dictionary<string, object>? propertyDict = JsonSerializer.Deserialize<Dictionary<string, object>>(update);
        if (topic == null || propertyDict == null) { return; }
        foreach (KeyValuePair<string, object> kvp in propertyDict)
        {
          if (kvp.Value == null)
          {
            topic.Properties.Remove(kvp.Key);
          }
          else if (topic.Properties.ContainsKey(kvp.Key))
          {
            topic.Properties[kvp.Key] = kvp.Value;
          }
        }
      }
      catch (Exception e)
      {
        Log($"ERROR: Failed to parse properties: {e.Message}");
        return;
      }
    }

    private void HandleRtt() {}

    private void HandleJson (byte[] bytes, int count)
    {
      try 
      {
        string text = Encoding.UTF8.GetString(bytes, 0, count);
        JsonDocument jsonDocument = JsonDocument.Parse(text);
        
        if (jsonDocument.RootElement.ValueKind != JsonValueKind.Array)
        {
          Log("Ignoring message: Recieved a non-array of JSON objects.");
          return;
        }

        foreach (JsonElement json in jsonDocument.RootElement.EnumerateArray())
        {
          //Validate JSON
          if (json.ValueKind != JsonValueKind.Object){
            Log("Ignoring message: JSON is not an object.");
            return;
          }
          // Validate "method" key
          if (json.TryGetProperty("method", out JsonElement method))
          {
            if (method.ValueKind != JsonValueKind.String)
            {
              Log("Ignoring message: JSON value of \"method\" is not a string.");
              return;
            }
            else
            {
              string? methodString = method.GetString();
              if (methodString is null)
              {
                Log("Ignoring message: JSON value of \"method\" is null.");
                return;
              }
              if (methodString != "announce" && methodString != "unannounce" && methodString != "properties")
              {
                Log($"Ignoring message: Unknown method \"{methodString}\".");
                return;
              }
            }
          }
          else
          {
            Log("Ignoring message: JSON does not contain a \"method\" key.");
            return;
          }

          // Validate "params" key
          if (json.TryGetProperty("params", out JsonElement parameters))
          {
            if (parameters.ValueKind != JsonValueKind.Object)
            {
              Log("Ignoring message: JSON value of \"params\" is not an object.");
              return;
            }
          }
          else
          {
            Log("Ignoring message: JSON does not contain a \"params\" key.");
            return;
          }

          // Log($"Received JSON: {json}");

          if (method.GetString() == "announce")
          {
            HandleAnnounce(parameters);
          }
          else if (method.GetString() == "unannounce")
          {
            HandleUnannounce(parameters);
          }
          else if (method.GetString() == "properties")
          {
            HandleProperties(parameters);
          }
          Log($"Received JSON: {json}");
        }
      }
      catch (Exception e)
      {
        Log($"ERROR: Failed to parse JSON: {e.Message}");
        return;
      }
    }

    private void HandleBinary (byte[] bytes)
    {
      try
      {
        object[] data = MessagePackSerializer.Deserialize<object[]>(bytes);

        // Validate MessagePack data
        if (data.Length != 4)
        {
          Log("Ignoring message: MessagePack data does not contain 4 elements.");
          return;
        }

        int topicId;
        long timestamp;
        int dataType;
        var dataValue = data[3];

        if (data[0] is sbyte sbyteTopicId)
        {
          topicId = sbyteTopicId;
        }
        else if (data[0] is byte byteTopicId)
        {
          topicId = byteTopicId;
        }
        else
        {
          Log($"Ignoring message: topicId is an invalid type ({data[0].GetType()}).");
          return;
        }

        if (data[1] is UInt64 uintTimestamp)
        {
          timestamp = (long)uintTimestamp;
        }
        else if (data[1] is UInt32 intTimestamp)
        {
          timestamp = intTimestamp;
        }
        else
        {
          Log($"Ignoring message: timestamp is an invalid type ({data[1].GetType()}).");
          return;
        }

        if (data[2] is byte byteDataType)
        {
          dataType = byteDataType;
        }
        else
        {
          Log($"Ignoring message: dataType is an invalid type ({data[2].GetType()}).");
          return;
        }

        Log($"{topicId}, {timestamp}, {dataType}, {dataValue}");
        if (serverTopics.Any(t => t.Id == topicId))
        {
          // Log("in server topics");
          serverTopics.First(t => t.Id == topicId).Value = dataValue;
          
        }
        else if (topicId == -1)
        {
          HandleTimestamp(timestamp);
        }
        // Code block to handle valid data
        // Log($"Received binary data: {data[0]} {data[1]} {data[2]} {data[3]}");
      }
      catch (Exception e)
      {
        Log($"ERROR: Failed to deserialize MessagePack data: {e.Message}");
        return;
      }
    }
    

    // UTILS
    private void Log(string message)
    {
      Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] [{name}] {message}");
    }
    private static int GetNewUID()
    {
      Random random = new();
      return random.Next(99999999);
    }

    private static int TypeLookup(string type)
    {
      Dictionary<string, int> typeToInt = new()
      {
        { "bool", 0 },
        { "double", 1 },
        { "int", 2 },
        { "float", 3 },
        { "string", 4 },
        { "byte[]", 5 },
        { "bool[]", 16 },
        { "double[]", 17 },
        { "int[]", 18 },
        { "float[]", 19 },
        { "string[]", 20 }
      };
      return typeToInt[type];
    }
  }
}