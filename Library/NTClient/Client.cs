using System.Net.WebSockets;
using System.Text;

namespace NTClient
{
  public class Client
  {
    private readonly int port = 5810; // Default NT4 port
    private readonly string ip = "localhost"; // Default NT4 address
    private readonly string name = "Client";
    private ClientWebSocket? client;

    private Subscriber[] subscribers = new Subscriber[0];

    public Client(string ip, string name)
    {
      this.ip = ip;
      this.name = name;
    }

    public void Connect()
    {
      Console.WriteLine("Connecting to server...");
      client = new ClientWebSocket();
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
    }

    public void Disconnect()
    {
      if (client == null) return;
      client.CloseAsync(WebSocketCloseStatus.NormalClosure, "Client disconnected", default).Wait();
      client.Dispose();
      client = null;
    }

    public bool IsConnected => client?.State == WebSocketState.Open;

    public void Subscribe(Topic[] topics)
    {
      if (client == null) return;
      Subscriber sub = new Subscriber();
      subscribers.Append(sub);
      sub.Topics = topics;
      sub.Uid = GetNewUID();

      SendJSON("subscribe", sub.ToSubscribe());
    }
    public byte[] ReceiveData()
    {
      if (client == null || !IsConnected) return null;
      
      var buffer = new byte[1024];
      var result = client.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None).GetAwaiter().GetResult();
      
      if (result.Count == 0)
      {
        return null;
      }
      
      var data = new byte[result.Count];
      Array.Copy(buffer, data, result.Count);
      
      return data;
    }

    public void SendJSON(string method, object parameters)
    {
      if (client == null || !IsConnected) return;
      string json = $"{{\"method\":\"{method}\",\"params\":{parameters}}}";
      byte[] bytes = Encoding.UTF8.GetBytes(json);
      client.SendAsync(new ArraySegment<byte>(bytes), WebSocketMessageType.Text, true, default).Wait();
    }

    public void SendBinary(byte[] data)
    {
      if (client == null || !IsConnected) return;
      client.SendAsync(new ArraySegment<byte>(data), WebSocketMessageType.Binary, true, default).Wait();
    }


    private int GetNewUID()
    {
      Random random = new Random();
      return random.Next(99999999);
    }
  }
}