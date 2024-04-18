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
    private readonly string name = "Client";
    private ClientWebSocket client;

    private readonly Subscriber[] subscribers = Array.Empty<Subscriber>();

    public Client(string ip, string name)
    {
      this.ip = ip;
      this.name = name;
      client = new ClientWebSocket();
    }

    public async void Connect()
    {
      Console.WriteLine("Connecting to server...");
      client.Options.AddSubProtocol("networktables.first.wpi.edu"); //v4.1.networktables.first.wpi.edu
      var address = $"ws://{ip}:{port}/nt/{name}";
      try{
        client.ConnectAsync(new Uri(address), default).Wait();
      }
      catch (Exception e){
        Console.WriteLine($"Failed to connect to server: {e.Message}");
        return;
      }
      Console.WriteLine("Connected to server.");
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
      SendJSON("subscribe", sub.ForSubscribe());
      Console.WriteLine("Subscribed to topic: " + topic);
    }

    public void Publish(Topic topic)
    {
      topic.Uid = GetNewUID();
      SendJSON("publish", topic.ForPublish());
    }

    public void SendTimestamp()
    {
      long time = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() * 1000;
      var txData = new object[] { -1, 0, 1, time };
      byte[] encodedData = MessagePackSerializer.Serialize(txData);
      SendBinary(encodedData);
    }

    public async Task StartListener()
    {
      var buffer = new ArraySegment<byte>(new byte[1024]);
      while (true)
      {
        WebSocketReceiveResult result = await client.ReceiveAsync(buffer, default);
        if (result?.MessageType == WebSocketMessageType.Close)
        {
          Console.WriteLine("Connection closed by server.");
          break;
        }
        else if (result?.MessageType == WebSocketMessageType.Text)
        {
          Console.WriteLine("Received text data.");
          string message = Encoding.UTF8.GetString(buffer.Array ?? Array.Empty<byte>(), 0, result.Count);
          Console.WriteLine("Received text data: " + message);
        }
        else if (result?.MessageType == WebSocketMessageType.Binary)
        {
          Console.WriteLine("Received binary data.");
          var t = MessagePackSerializer.Deserialize<object[]>(buffer.Array);
          Console.WriteLine("Deserialized binary data: " + t[1]);
        }
      }
    }

    public void SendJSON(string method, object parameters)
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
      // string json = $"{{\"method\":\"{method}\",\"params\":{parameters}}}";
      Console.WriteLine($"Sending: {json}");
      byte[] bytes = Encoding.UTF8.GetBytes(json);

      try
      {
        client.SendAsync(new ArraySegment<byte>(bytes), WebSocketMessageType.Text, true, default).Wait();
        Console.WriteLine("SendJSON succeeded.");
      }
      catch (Exception ex)
      {
        Console.WriteLine($"SendJSON failed: {ex.Message}");
      }
    }

    public void SendBinary(byte[] data)
    {
      if (client == null || !IsConnected) return;
      client.SendAsync(new ArraySegment<byte>(data), WebSocketMessageType.Binary, true, default).Wait();
    }

    private static int GetNewUID()
    {
      Random random = new Random();
      return random.Next(99999999);
    }
  }
}