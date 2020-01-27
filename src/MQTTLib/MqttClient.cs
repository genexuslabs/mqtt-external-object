using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using MQTTnet;
using MQTTnet.Client;
using MQTTnet.Client.Options;
using MQTTnet.Formatter;
using Org.BouncyCastle.Asn1;
using Org.BouncyCastle.Asn1.Pkcs;
using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.Security;
using Org.BouncyCastle.Utilities.IO.Pem;

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
			IMqttClient client = new MqttFactory().CreateMqttClient();

			var b = new MqttClientOptionsBuilder()
				.WithTcpServer(url, config.Port)
				.WithClientId(config.ClientId)
				.WithKeepAlivePeriod(TimeSpan.FromSeconds(config.KeepAlive))
				.WithMaximumPacketSize(Convert.ToUInt32(config.BufferSize))
				.WithCommunicationTimeout(TimeSpan.FromSeconds(config.WaitTimeout));

			if (!config.SSLConnection && !string.IsNullOrEmpty(config.UserName) && !string.IsNullOrEmpty(config.Password))
				b = b.WithCredentials(config.UserName, config.Password);

			switch (config.ProtocolVersion)
			{
				case 310:
					b = b.WithProtocolVersion(MqttProtocolVersion.V310);
					break;
				case 311:
					b = b.WithProtocolVersion(MqttProtocolVersion.V311);
					break;
				case 500:
					b = b.WithProtocolVersion(MqttProtocolVersion.V500);
					break;
				default:
					throw new InvalidDataException("Invalid protocol versions. Valid versions are 310, 311 or 500");
			}


			if (config.SSLConnection)
			{
				string base64CACert = RSAKeys.GetPlainBase64(config.CAcertificate);
				X509Certificate2 caCert = new X509Certificate2(Convert.FromBase64String(base64CACert));
				byte[] caBytes = caCert.Export(X509ContentType.Cert);

				string base64Client = RSAKeys.GetPlainBase64(config.ClientCertificate);
				X509Certificate2 cliCert = new X509Certificate2(Convert.FromBase64String(base64Client), config.ClientCerificatePassphrase);
				cliCert = cliCert.CopyWithPrivateKey(RSAKeys.ImportPrivateKey(config.PrivateKey, new PasswordFinder(config.ClientCerificatePassphrase)));
				byte[] cliBytes = cliCert.Export(X509ContentType.Pfx);

				try
				{
					var tls = new MqttClientOptionsBuilderTlsParameters
					{
						SslProtocol = System.Security.Authentication.SslProtocols.Tls12 | System.Security.Authentication.SslProtocols.Tls11 | System.Security.Authentication.SslProtocols.Tls,
						UseTls = true,
						AllowUntrustedCertificates = true,
						IgnoreCertificateChainErrors = true,
						IgnoreCertificateRevocationErrors = true,

						Certificates = new List<byte[]>() { caBytes, cliBytes },

						CertificateValidationCallback = (certificate, chain, sslError, opts) => true

					};

					b = b.WithTls(tls);

				}
				finally
				{
					caCert.Dispose();
					cliCert.Dispose();
				}
			}

			client.ConnectAsync(b.Build()).Wait();

			MqttClient mqtt = new MqttClient(client);

			Guid key = Guid.NewGuid();

			s_instances[key] = mqtt;

			return key;
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

			string fileName = $"a{gxproc}.dll";
			string baseDir = !string.IsNullOrEmpty(AppDomain.CurrentDomain.RelativeSearchPath) ? AppDomain.CurrentDomain.RelativeSearchPath : AppDomain.CurrentDomain.BaseDirectory;
			string fullPath = Path.Combine(baseDir, fileName);
			if (!File.Exists(fullPath))
				throw new FileNotFoundException($"File not found at {fullPath}", fileName);

			MqttClient mqtt = GetClient(key);
			var a = mqtt.m_mqttClient.SubscribeAsync(topic).Result;
			mqtt.m_mqttClient.UseApplicationMessageReceivedHandler(msg =>
			{
				if (msg == null || msg.ApplicationMessage == null || msg.ApplicationMessage.Payload == null)
					return;

				Console.WriteLine($"Message arrived! Topic:{msg.ApplicationMessage.Topic} Payload:{Encoding.UTF8.GetString(msg.ApplicationMessage.Payload)}");

				Assembly asm = Assembly.LoadFrom(fullPath);
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

		public static void Unsubscribe(Guid key, string topic)
		{
			MqttClient mqtt = GetClient(key);
			mqtt.m_mqttClient.UnsubscribeAsync(topic).Wait();
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
