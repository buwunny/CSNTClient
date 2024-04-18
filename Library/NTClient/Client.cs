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
    private ClientWebSocket client;

    private Topic[] clientTopics = [];
    private Topic[] serverTopics = [];

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

    public bool IsConnected => client?.State == WebSocketState.Open;

    public void Subscribe(string topic)
    {
      if (client == null) return;
      Subscriber sub = new Subscriber(topic, GetNewUID(), new SubscriptionOptions());
      subscribers.Append(sub);
      SendJson("subscribe", sub.GetSubscribeObject());
      Log("Subscribed to topic: \"" + topic + "\"");
    }

    public void Unsubscribe(int subuid)
    {
      if (client == null) return;
      Subscriber? sub = subscribers.FirstOrDefault(s => s.Uid == subuid);
      if (sub == null)
      {
        Log($"Subscriber with UID {subuid} not found.");
        return;
      };
      SendJson("unsubscribe", sub.GetUnsubscribeObject());
      Log("Unsubscribed from topic: \"" + sub.Topics[0] + "\"");
      subscribers = subscribers.Where(s => s.Uid != subuid).ToArray();
    }

    public void Publish(string type, string topic)
    {
      Topic newTopic = new Topic();
      newTopic.Name = topic;
      newTopic.Uid = GetNewUID();
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
        else if (result?.MessageType == WebSocketMessageType.Text)
        {
          if (buffer.Array != null)
          {
            HandleJson(buffer.Array, result.Count);
          }
          else
          {
            Log("Received empty text message.");
          }
        }
        else if (result?.MessageType == WebSocketMessageType.Binary)
        {
          object[] t = MessagePackSerializer.Deserialize<object[]>(buffer.Array);
          HandleBinary(t);
        }
      }
    }

    private void HandleJson (byte[] bytes, int count)
    {
      try 
      {
        string text = Encoding.UTF8.GetString(bytes, 0, count);
        JsonDocument jsonDocument = JsonDocument.Parse(text);
        
        if (jsonDocument.RootElement.ValueKind != JsonValueKind.Array)
        {
          throw new Exception(@"
            Recieved a non-array of JSON objects.
            ""Each WebSockets text data frame shall consist of a list of JSON objects.""
            https://github.com/wpilibsuite/allwpilib/blob/main/ntcore/doc/networktables4.adoc#text-frames"
          );
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
          // {}
          // else if (method.GetString() == "unannounce")
          // {}
          // else if (method.GetString() == "properties")
          // {}
          Log($"Received JSON: {json}");
        }
      }
      catch (Exception e)
      {
        Log($"ERROR: Failed to parse JSON: {e.Message}");
        return;
      }
    }

    private void HandleBinary (object[] data)
    {
      Log($"Received binary data: {data[0]} {data[1]} {data[2]} {data[3]}");
      
    }
    

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