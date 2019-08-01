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
                MQTTLib.MqttClient client = Common.CommonConnection.Connect();

                string topic = ConfigurationManager.AppSettings["topic"];

                client.Subscribe(topic, "SaveMessage", 0);

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
