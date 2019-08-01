using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using MQTTnet;
using MQTTnet.Client;
using MQTTnet.Client.Options;

namespace MQTTLib
{
    public class MqttClient
    {
        IMqttClient m_mqttClient;
        public MqttClient(IMqttClient mqttClient)
        {
            m_mqttClient = mqttClient;
        }

        public MqttClient() { }

        public static MqttClient Connect(string url, MqttConfig config)
        {
            MqttFactory factory = new MqttFactory();
            IMqttClient client = factory.CreateMqttClient();


            var b = new MqttClientOptionsBuilder();
            var options = b.WithTcpServer(url, config.Port)
                .WithClientId(config.ClientId)
                .WithKeepAlivePeriod(TimeSpan.FromSeconds(config.KeepAlive))
                .WithMaximumPacketSize(Convert.ToUInt32(config.BufferSize))
                .WithCommunicationTimeout(TimeSpan.FromMilliseconds(config.ConnectionTimeout))
                .WithCredentials(config.UserName, config.Password)
                .Build();

            //if (config.SSLConnection)
            //    b = b.WithTls();

            //options = b.Build();

            client.ConnectAsync(options).Wait();


            return new MqttClient(client);
        }

        public void Disconnect()
        {
            m_mqttClient.DisconnectAsync().Wait();
            m_mqttClient.Dispose();
        }

        public void Subscribe(string topic, string gxproc, int qos)
        {
            if (string.IsNullOrEmpty(gxproc))
                throw new ArgumentNullException(nameof(gxproc), "GeneXus procedure parameter cannot be null");

            var a = m_mqttClient.SubscribeAsync(topic).Result;
            m_mqttClient.UseApplicationMessageReceivedHandler(msg =>
            {
                Console.WriteLine($"Message arrived! Topic:{msg.ApplicationMessage.Topic} Payload:{Encoding.UTF8.GetString(msg.ApplicationMessage.Payload)}");

                string dllFile = $"a{gxproc}.dll";
                if (!File.Exists(dllFile))
                    throw new FileNotFoundException($"File {dllFile} not found.", dllFile);

                Assembly asm = Assembly.LoadFrom(dllFile);
                Type procType = asm.GetTypes().FirstOrDefault(t => t.FullName.EndsWith(gxproc, StringComparison.InvariantCultureIgnoreCase));

                if (procType == null)
                    throw new InvalidDataException("Data type not found");

                var methodInfo = procType.GetMethod("execute", new Type[] { typeof(string), typeof(string) });
                if (methodInfo == null)
                    throw new NotImplementedException("Method 'execute' not found");

                var obj = Activator.CreateInstance(procType);

                methodInfo.Invoke(obj, new object[] { msg.ApplicationMessage.Topic, Encoding.UTF8.GetString(msg.ApplicationMessage.Payload) });

            });
        }

        public void Publish(string topic, string payload, int qos, bool retainMessage)
        {
            m_mqttClient.PublishAsync(topic, payload, (MQTTnet.Protocol.MqttQualityOfServiceLevel)Enum.ToObject(typeof(MQTTnet.Protocol.MqttQualityOfServiceLevel), qos), retainMessage).Wait();
        }
    }
}
