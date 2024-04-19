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
      await WebsocketListener();
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

    private async Task WebsocketListener()
    {
      var buffer = new ArraySegment<byte>(new byte[1024]);
      while (true)
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

    private async Task WebsocketSender()
    {
    }

    // CLIENT -> SERVER

    // Publish Messages (Client to Server)
    public void Publish(string type, string topic)
    {
      Topic newTopic = new Topic();
      newTopic.Name = topic;
      newTopic.PubUid = GetNewUID();
      newTopic.Type = type;
      SendJson("publish", newTopic.GetPublishObject());
      clientTopics.Append(newTopic);
    }

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
    
    public void Subscribe(string topic)
    {
      if (client == null) return;
      Subscriber sub = new Subscriber([topic], GetNewUID(), new SubscriptionOptions());
      subscribers.Append(sub);
      SendJson("subscribe", sub.GetSubscribeObject());
      Log("Subscribed to topic: \"" + topic + "\"");
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

    private void SendTimestamp()
    {
      long time = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() * 1000;
      var txData = new object[] { -1, 0, 1, time };
      byte[] encodedData = MessagePackSerializer.Serialize(txData);
      SendBinary(encodedData);
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
    private void HandleAnnounce() {}

    private void HandleUnannounce() {}

    private void HandleProperties() {}

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

          // TODO: Handle different methods
          // if (method.GetString() == "announce")
          // {
          //   string? name = parameters.GetProperty("name").GetString();
          //   int id = parameters.GetProperty("id").GetInt32();
          //   string? type = parameters.GetProperty("type").GetString();
          //   int pubUid = parameters.TryGetProperty("pubuid", out JsonElement pubUidElement) ? pubUidElement.GetInt32() : 0;
          //   Dictionary<string, object>? properties = JsonSerializer.Deserialize<Dictionary<string, object>>(parameters.GetProperty("properties").GetRawText());
            
          //   if (name is null || type is null || properties is null)
          //   {
          //     Log("Ignoring message: JSON does not contain a valid \"name\", \"type\", or \"properties\" key.");
          //     return;
          //   }
          //   Topic announcedTopic = new Topic(name, pubUid, type, properties);

          //   serverTopics[parameters.GetProperty("id").GetInt32()] = announcedTopic;
          // }
          // else if (method.GetString() == "unannounce")
          // {

          //   int id = parameters.GetProperty("id").GetInt32();
          //   if (serverTopics.Any(topic => topic.ServerTopicId == id))
          //   {
          //     // Code to handle when a topic with the given id exists in serverTopics
          //     serverTopics.(id);
          //   }
          //   else
          //   {
          //     Log($"Ignoring message: serverTopic with ID {id} not found.");
          //   }
          // }
          // else if (method.GetString() == "properties")
          // {
            
          // }
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


        Dictionary<int, Type> intToType = new()
        {
          { 0, typeof(bool) },
          { 1, typeof(double) },
          { 2, typeof(int) },
          { 3, typeof(float) },
          { 4, typeof(string) },
          { 5, typeof(byte[]) },
          { 16, typeof(bool[]) },
          { 17, typeof(double[]) },
          { 18, typeof(int[]) },
          { 19, typeof(float[]) },
          { 20, typeof(string[]) }
        };

        int topicId;
        int timestamp;
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

        if (data[1] is uint uintTimestamp)
        {
          timestamp = (int)uintTimestamp;
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
        if (serverTopics.Any(t => t.PubUid == topicId))
        {
          Log("in server topics");
        }
        else if (topicId == -1)
        {
          // handleRtt()
        }
        // Code block to handle valid data
        Log($"Received binary data: {data[0]} {data[1]} {data[2]} {data[3]}");
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
  }
}