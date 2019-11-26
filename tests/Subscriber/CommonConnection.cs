using System;
using System.Configuration;
using MQTTLib;

namespace Common
{
    internal class CommonConnection
    {
        public static Guid ConnectTLS()
        {
            MqttConfig config = new MqttConfig
            {
                ClientCerificatePassphrase = @"genexus",
                SSLConnection = true,
                Port = 8884,
                CAcertificatePath = @"C:\code\genexus\MQTT\tests\OpenSSL\mosquitto.org.crt",
                ClientCertificatePath = @"C:\code\genexus\MQTT\tests\OpenSSL\cli.pfx"
            };

            
            //config.PrivateKey = @"";

            Guid key = MQTTLib.MqttClient.Connect("test.mosquitto.org", config);

            Console.WriteLine("Connected!");

            return key;
        }

        public static Guid Connect()
        {
            MqttConfig config = GetConfig();

            return Connect(config);
        }

        static Guid Connect(MqttConfig config)
        {
            string url = ConfigurationManager.AppSettings["url"];

            Guid key = MQTTLib.MqttClient.Connect(url, config);

            Console.WriteLine("Connected!");

            return key;
        }

        public static MqttConfig GetConfig()
        {
            string user = ConfigurationManager.AppSettings["user"];
            string password = ConfigurationManager.AppSettings["password"];

            MqttConfig config = new MQTTLib.MqttConfig
            {
                UserName = user,
                Password = password,
            };

            return config;
        }
    }
}
