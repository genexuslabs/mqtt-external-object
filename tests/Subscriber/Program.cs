using MQTTLib;
using System;
using System.Configuration;

namespace Subscriber
{
	class Program
	{
		static void Main(string[] args)
		{
			try
			{
				MqttConfig config = Common.CommonConnection.GetConfig();

				//Guid key = Common.CommonConnection.ConnectTLS();


				//config.CleanSession = false;
				//config.ClientId = "MyTest";

				Guid key = Common.CommonConnection.Connect(config);

				string topic = ConfigurationManager.AppSettings["topic"];


				Console.WriteLine($"Press <enter> to subscribe...");
				Console.ReadLine();

				MqttStatus status = MqttClient.Subscribe(key, "/test", "SaveMessage", 1);

				if (status.Error)
					throw new Exception(status.ErrorMessage);

				Console.WriteLine($"Subscribed to topic:/test");
				Console.WriteLine($"Press <enter> to subscribe (again)...");

				Console.ReadLine();

				status = MqttClient.Subscribe(key, "/test2", "SaveMessage2", 1);

				if (status.Error)
					throw new Exception(status.ErrorMessage);

				Console.WriteLine($"Subscribed to topic:/test2");

				Console.WriteLine($"Press <enter> to exit...");
				Console.ReadLine();
				MqttClient.Disconnect(key);
			}
			catch (Exception ex)
			{
				while (ex != null)
				{
					Console.WriteLine(ex.Message);
					ex = ex.InnerException;
				}
			}
		}
	}
}
