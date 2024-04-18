using NTClient;
class TestClient {
	public static void Main(string[] args)
	{
		// Create a client with a specific IP address
		var client = new Client("127.0.0.1", "Test");

		// Connect the client
    client.Connect();
 
    // client.Subscribe([topic]);
    client.Subscribe("");
    // client.Subscribe("/datatable/x");
		
		client.Publish(new Topic());
		while (client.IsConnected)
		{
			// client.SendTimestamp();
			// Print the connection status
			// Console.WriteLine("IsConnected: " + client.IsConnected);
      // client.SendTimestamp();
			// Wait for half a second
			Thread.Sleep(500);
		}
		// Keep the console open until a key is pressed
		//Console.WriteLine("Press any key to exit...");
		//Console.ReadKey();
	}

}