using System;
using System.Configuration;

namespace Common
{
    internal class CommonConnection
    {
        public static MQTTLib.MqttClient Connect()
        {
            string url = ConfigurationManager.AppSettings["url"];
            string user = ConfigurationManager.AppSettings["user"];
            string password = ConfigurationManager.AppSettings["password"];

            MQTTLib.MqttConfig config = new MQTTLib.MqttConfig
            {
                UserName = user,
                Password = password
            };

            MQTTLib.MqttClient client = MQTTLib.MqttClient.Connect(url, config);

            Console.WriteLine("Connected!");

            return client;
        }
    }
}
