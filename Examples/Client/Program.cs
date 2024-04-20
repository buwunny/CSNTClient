﻿using NTClient;
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
		
		client.Publish("int", "TestTopic");
		// client.SetProperties("TestTopic", new Dictionary<string, object> { { "retained", false } });
		// client.Unpublish("TestTopic");
		// int i = 0;
		while (client.IsConnected)
		{
			// client.set("TestTopic", i);
			// i ++;
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