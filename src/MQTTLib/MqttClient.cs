using System;
using System.Collections.Generic;
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
		static Dictionary<Guid, MqttClient> s_instances = new Dictionary<Guid, MqttClient>();
        readonly IMqttClient m_mqttClient;
        public MqttClient(IMqttClient mqttClient)
        {
            m_mqttClient = mqttClient;
        }

        public MqttClient() { }

        public static Guid Connect(string url, MqttConfig config)
        {
            MqttFactory factory = new MqttFactory();
            IMqttClient client = factory.CreateMqttClient();

            var b = new MqttClientOptionsBuilder()
                .WithTcpServer(url, config.Port)
                .WithClientId(config.ClientId)
                .WithKeepAlivePeriod(TimeSpan.FromSeconds(config.KeepAlive))
                .WithMaximumPacketSize(Convert.ToUInt32(config.BufferSize))
                .WithCommunicationTimeout(TimeSpan.FromSeconds(config.WaitTimeout))
                .WithCredentials(config.UserName, config.Password);

            if (config.SSLConnection)
            {
                byte[] buffer = Convert.FromBase64String(config.CertificateKey);

                var tls = new MqttClientOptionsBuilderTlsParameters
                {
                    UseTls = true,
                    //AllowUntrustedCertificates = true,
                    Certificates = new List<byte[]> { buffer },
                    CertificateValidationCallback = (certificate, chain, sslError, opts) =>
                    {
                        if (sslError != System.Net.Security.SslPolicyErrors.None)
                            return false;

                        if (!ByteArrayCompare(certificate.GetRawCertData(), opts.ChannelOptions.TlsOptions.Certificates[0]))
                            return false;

                        foreach (var chainElement in chain.ChainElements)
                        {
                            if (!chainElement.Certificate.Verify())
                                return false;
                        }

                        //CAAuthorityKey validation missing
                        //PrivateKey validation missing
                        //ClientKeyPassphrase validation missing
                        
                        return true;
                    }
                    //IgnoreCertificateChainErrors = false,
                    //IgnoreCertificateRevocationErrors = false
                };



                b = b.WithTls(tls);
            }

            client.ConnectAsync(b.Build()).Wait();

			MqttClient mqtt = new MqttClient(client);
			Guid key = Guid.NewGuid();

			s_instances[key] = mqtt;

			return key;
        }

        static bool ByteArrayCompare(ReadOnlySpan<byte> a1, ReadOnlySpan<byte> a2)
        {
            return a1.SequenceEqual(a2);
        }

        public static void Disconnect(Guid key)
        {
			MqttClient mqtt = GetClient(key);
			mqtt.m_mqttClient.DisconnectAsync().Wait();
            mqtt.m_mqttClient.Dispose();
        }

        public static void Subscribe(Guid key, string topic, string gxproc, int qos)
        {
			if (string.IsNullOrEmpty(gxproc))
                throw new ArgumentNullException(nameof(gxproc), "GeneXus procedure parameter cannot be null");

            string dllFile = $"a{gxproc}.dll";
            if (!File.Exists(dllFile))
                throw new FileNotFoundException($"File {dllFile} not found.", dllFile);

			MqttClient mqtt = GetClient(key);
			var a = mqtt.m_mqttClient.SubscribeAsync(topic).Result;
			mqtt.m_mqttClient.UseApplicationMessageReceivedHandler(msg =>
            {
                if (msg == null || msg.ApplicationMessage == null || msg.ApplicationMessage.Payload == null)
                    return;

                Console.WriteLine($"Message arrived! Topic:{msg.ApplicationMessage.Topic} Payload:{Encoding.UTF8.GetString(msg.ApplicationMessage.Payload)}");

                dllFile = $"a{gxproc}.dll";
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

        public static void Publish(Guid key, string topic, string payload, int qos, bool retainMessage)
        {
			MqttClient mqtt = GetClient(key);
			mqtt.m_mqttClient.PublishAsync(topic, payload, (MQTTnet.Protocol.MqttQualityOfServiceLevel)Enum.ToObject(typeof(MQTTnet.Protocol.MqttQualityOfServiceLevel), qos), retainMessage).Wait();
        }

		static MqttClient GetClient(Guid key)
		{
			if (string.IsNullOrEmpty(key.ToString()))
				throw new ArgumentNullException(nameof(key), "The key cannot be null");

			if (!s_instances.ContainsKey(key))
				throw new ArgumentOutOfRangeException(nameof(key), "Connection is not open.");

			return s_instances[key];
		}
    }
}
