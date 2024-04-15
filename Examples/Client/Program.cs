using DNT4;
class TestClient {
	public static void Main(string[] args)
	{
		// Create a client with a specific IP address
		var client = new Client("127.0.0.1", "Test");

		// Connect the client
		client.Connect();
		while (client.IsConnected)
		{
			// Print the connection status
			Console.WriteLine("IsConnected: " + client.IsConnected);

			// Wait for half a second
			Thread.Sleep(500);
		}
		// Keep the console open until a key is pressed
		//Console.WriteLine("Press any key to exit...");
		//Console.ReadKey();
	}

}