using MQTTLib;
using System;
using System.Configuration;

namespace Publisher
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

				Console.WriteLine($"Enter the message you want to send, type exit to quit.");

				string command = Console.ReadLine();
				while (command != "exit")
				{
					MqttStatus status=  MqttClient.Publish(key, topic, command, 2, true, 1);
					if (status.Error)
						throw new Exception(status.ErrorMessage);

					command = Console.ReadLine();
				}

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
