using System;
using System.Configuration;
using MQTTLib;

namespace Common
{
	internal class CommonConnection
	{
		public static Guid ConnectTLS()
		{
			string passphrase = ConfigurationManager.AppSettings["passphrase"];
			string caCertificate = ConfigurationManager.AppSettings["caCertificate"];
			string clientCertificate = ConfigurationManager.AppSettings["clientCertificate"];
			string privateKey = ConfigurationManager.AppSettings["privateKey"];

			MqttConfig config = new MqttConfig
			{
				ClientCerificatePassphrase = passphrase,
				SSLConnection = true,
				Port = 8884,
				CAcertificate = caCertificate,
				ClientCertificate = clientCertificate,
				PrivateKey = privateKey
			};

			MqttStatus status = MQTTLib.MqttClient.Connect("test.mosquitto.org", config);

			if (!status.Error)
				Console.WriteLine("Connected!");
			else
				Console.WriteLine($"Error:{status.ErrorMessage}");

			return status.Key;
		}

		public static Guid Connect()
		{
			MqttConfig config = GetConfig();

			return Connect(config);
		}

		static Guid Connect(MqttConfig config)
		{
			string url = ConfigurationManager.AppSettings["url"];

			MqttStatus status = MQTTLib.MqttClient.Connect(url, config);

			if (!status.Error)
				Console.WriteLine("Connected!");
			else
				Console.WriteLine($"Error:{status.ErrorMessage}");

			return status.Key;
		}

		public static MqttConfig GetConfig()
		{
			string user = "";// ConfigurationManager.AppSettings["user"];
			string password = ConfigurationManager.AppSettings["password"];

			MqttConfig config = new MQTTLib.MqttConfig
			{
				UserName = user,
				Password = password
			};

			return config;
		}
	}
}
