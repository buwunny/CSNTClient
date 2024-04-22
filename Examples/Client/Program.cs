using NTClient;
class TestClient {
	public static void Main(string[] args)
	{
		// Create a client with a specific IP address
		var client = new Client("127.0.0.1", "Test");

		// Connect the client
    client.Connect();
 
    // client.Subscribe(["/datatable/x", "/datatable/y"]);
		client.Subscribe(["/datatable/y"]);
    client.Subscribe(["/datatable/x"]);
		
		client.Publish("int", "TestTopic");
    // client.Subscribe(["TestTopic"]);
		// client.Unpublish("TestTopic");
		int i = 0;
		while (client.IsConnected)
		{
			Thread.Sleep(500);
			client.UpdateTopic("TestTopic", i);
			// client.SendTimestamp();
			Console.WriteLine("values: " + client.GetTopicValue("/datatable/x") + " " + client.GetTopicValue("/datatable/y"));
			i ++;
			// client.SendTimestamp();
			// Print the connection status
			// Console.WriteLine("IsConnected: " + client.IsConnected);
      // client.SendTimestamp();
			// Wait for half a second
		}
		// Keep the console open until a key is pressed
		//Console.WriteLine("Press any key to exit...");
		//Console.ReadKey();
	}

}