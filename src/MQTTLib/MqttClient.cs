using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;
using MQTTnet;
using MQTTnet.Client;
using MQTTnet.Client.Options;
using MQTTnet.Formatter;

namespace MQTTLib
{
	public class MqttClient
	{
		static Dictionary<Guid, MqttClient> s_instances = new Dictionary<Guid, MqttClient>();
		readonly IMqttClient m_mqttClient;
		readonly MqttConfig m_config;
		public MqttClient(IMqttClient mqttClient, MqttConfig config)
		{
			m_mqttClient = mqttClient;
			m_config = config;
		}

		public MqttClient() { }

		private static readonly object balanceLock = new object();
		static Dictionary<Guid, MqttClient> Connections
		{
			get
			{
				lock (balanceLock)
					return s_instances;
			}
		}

		public static MqttStatus Connect(string url, MqttConfig config)
		{
			Guid key = Guid.NewGuid();

			try
			{
				IMqttClient client = new MqttFactory().CreateMqttClient();

				var b = new MqttClientOptionsBuilder()
					.WithTcpServer(url, config.Port)
					.WithKeepAlivePeriod(TimeSpan.FromSeconds(config.KeepAlive))
					.WithMaximumPacketSize(Convert.ToUInt32(config.BufferSize))
					.WithCommunicationTimeout(TimeSpan.FromSeconds(config.ConnectionTimeout))
					.WithCleanSession(!config.PersistentClientSession);


				if (!string.IsNullOrEmpty(config.ClientId))
					b = b.WithClientId(config.ClientId);

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


				client.UseDisconnectedHandler(async e =>
				{
					try
					{
						if (Connections.ContainsKey(key))
						{
							await Task.Delay(TimeSpan.FromSeconds(config.AutoReconnectDelay));
							await client.ReconnectAsync();
						}
					}
					catch
					{
						Connections.Remove(key);
						client.Dispose();
					}
				});

				client.ConnectAsync(b.Build()).Wait();

				MqttClient mqtt = new MqttClient(client, config);

				Connections[key] = mqtt;

			}
			catch (Exception ex)
			{
				return MqttStatus.Fail(Guid.Empty, ex);
			}

			return MqttStatus.Success(key);
		}

		public static MqttStatus Disconnect(Guid key)
		{
			try
			{
				MqttClient mqtt = GetClient(key);
				Connections.Remove(key);
				mqtt.m_mqttClient.DisconnectAsync(new MQTTnet.Client.Disconnecting.MqttClientDisconnectOptions() { ReasonCode = MQTTnet.Client.Disconnecting.MqttClientDisconnectReason.NormalDisconnection, ReasonString = "Disconnected by user" }).Wait();
				mqtt.m_mqttClient.Dispose();
			}
			catch (Exception ex)
			{
				return MqttStatus.Fail(key, ex);
			}

			return MqttStatus.Success(key);
		}

		public static MqttStatus Subscribe(Guid key, string topic, string gxproc, int qos)
		{
			try
			{
				if (string.IsNullOrEmpty(gxproc))
					throw new ArgumentNullException(nameof(gxproc), "GeneXus procedure parameter cannot be null");

				string fileName = $"a{gxproc}.dll";
				string baseDir = !string.IsNullOrEmpty(AppDomain.CurrentDomain.RelativeSearchPath) ? AppDomain.CurrentDomain.RelativeSearchPath : AppDomain.CurrentDomain.BaseDirectory;
				string fullPath = Path.Combine(baseDir, fileName);
				if (!File.Exists(fullPath))
					throw new FileNotFoundException($"File not found at {fullPath}", fileName);

				MqttClient mqtt = GetClient(key);

				if (topic.Contains("*") || topic.Contains("+") || topic.Contains("#"))
					if (!mqtt.m_config.AllowWildcardsInTopicFilters)
						throw new InvalidDataException("Wildcards not allowed for this instance.");

				var a = mqtt.m_mqttClient.SubscribeAsync(topic, (MQTTnet.Protocol.MqttQualityOfServiceLevel)Enum.ToObject(typeof(MQTTnet.Protocol.MqttQualityOfServiceLevel), qos)).Result;
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
			catch (Exception ex)
			{
				return MqttStatus.Fail(key, ex);
			}

			return MqttStatus.Success(key);
		}

		public static MqttStatus Unsubscribe(Guid key, string topic)
		{
			try
			{
				MqttClient mqtt = GetClient(key);
				mqtt.m_mqttClient.UnsubscribeAsync(topic).Wait();
			}
			catch (Exception ex)
			{
				return MqttStatus.Fail(key, ex);
			}

			return MqttStatus.Success(key);
		}

		public static MqttStatus Publish(Guid key, string topic, string payload, int qos, bool retainMessage)
		{
			try
			{
				MqttClient mqtt = GetClient(key);
				mqtt.m_mqttClient.PublishAsync(topic, payload, (MQTTnet.Protocol.MqttQualityOfServiceLevel)Enum.ToObject(typeof(MQTTnet.Protocol.MqttQualityOfServiceLevel), qos), retainMessage).Wait();
			}
			catch (Exception ex)
			{
				return MqttStatus.Fail(key, ex);
			}

			return MqttStatus.Success(key);
		}

		public static MqttStatus IsConnected(Guid key, out bool connected)
		{
			connected = false;
			try
			{
				MqttClient mqtt = GetClient(key);
				connected = mqtt.m_mqttClient.IsConnected;
			}
			catch (Exception ex)
			{
				return MqttStatus.Fail(key, ex);
			}
			return MqttStatus.Success(key);
		}

		static MqttClient GetClient(Guid key)
		{
			if (string.IsNullOrEmpty(key.ToString()))
				throw new ArgumentNullException(nameof(key), "The key cannot be null");

			if (!Connections.ContainsKey(key))
				throw new ArgumentOutOfRangeException(nameof(key), $"{key} does not hold a valid connection.");

			return Connections[key];
		}


	}
}
