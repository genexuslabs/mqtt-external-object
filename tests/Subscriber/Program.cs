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
				//Guid key = Common.CommonConnection.ConnectTLS();
				Guid key = Common.CommonConnection.Connect();

				string topic = ConfigurationManager.AppSettings["topic"];

				MqttStatus status = MqttClient.Subscribe(key, topic, "SaveMessage", 2);

				if (status.Error)
					throw new Exception(status.ErrorMessage);

				Console.WriteLine($"Subscribed to topic:{topic}");
				Console.WriteLine($"Press <enter> to exit...");
				Console.ReadLine();
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
