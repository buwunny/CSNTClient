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
  }
}