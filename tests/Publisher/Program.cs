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
                MQTTLib.MqttClient client = Common.CommonConnection.ConnectTLS();

                string topic = ConfigurationManager.AppSettings["topic"];

                Console.WriteLine($"Enter the message you want to send, type exit to quit.");

                string command = Console.ReadLine();
                while (command != "exit")
                {
                    client.Publish(topic, command, 2, true);
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
