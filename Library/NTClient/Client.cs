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

    private readonly Subscriber[] subscribers = Array.Empty<Subscriber>();

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
      await StartListener();
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
      SendJson("subscribe", sub.ForSubscribe());
      Log("Subscribed to topic: \"" + topic + "\"");
    }

    public void Publish(Topic topic)
    {
      topic.Uid = GetNewUID();
      SendJson("publish", topic.ForPublish());
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
        Log($"ERROR SendJSON failed: {ex.Message}");
      }
    }

    private void SendBinary(byte[] data)
    {
      if (client == null || !IsConnected) return;
      client.SendAsync(new ArraySegment<byte>(data), WebSocketMessageType.Binary, true, default).Wait();
    }

    private async Task StartListener()
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
          string message = Encoding.UTF8.GetString(buffer.Array ?? Array.Empty<byte>(), 0, result.Count);
          HandleJson(message);
        }
        else if (result?.MessageType == WebSocketMessageType.Binary)
        {
          object[] t = MessagePackSerializer.Deserialize<object[]>(buffer.Array);
          HandleBinary(t);
        }
      }
    }

    private void HandleJson (string json)
    {
      Log("Received JSON: " + json);
    }

    private void HandleBinary (object[] data)
    {
      Log($"Received binary data: {data[0]} {data[1]} {data[2]} {data[3]}");
      
    }
    

    private void Log(string message)
    {
      Console.WriteLine($"[{DateTime.Now:HH:mm:ss}][{name}] {message}");
    }
    private static int GetNewUID()
    {
      Random random = new();
      return random.Next(99999999);
    }
  }
}