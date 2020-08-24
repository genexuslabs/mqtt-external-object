using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization.Json;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using MQTTnet;
using MQTTnet.Client;
using MQTTnet.Client.Connecting;
using MQTTnet.Client.Options;
using MQTTnet.Client.Subscribing;
using MQTTnet.Formatter;
using MQTTnet.Packets;

namespace MQTTLib
{
	public class MqttClient
	{
		static Dictionary<Guid, MqttClient> s_instances = new Dictionary<Guid, MqttClient>();
		Dictionary<string, string> s_topicProcs = new Dictionary<string, string>();

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

		void AddTopic(string topic, string gxProc)
		{
			s_topicProcs[topic] = gxProc;
		}

		void RemoveTopic(string topic)
		{
			if (s_topicProcs.ContainsKey(topic))
				s_topicProcs.Remove(topic);
		}

		string GetProc(string topic)
		{
			if (s_topicProcs.ContainsKey(topic))
				return s_topicProcs[topic];

			return null;
		}

		IEnumerable<string> GetMatchTopicProcs(string topic)
		{
			foreach (string key in s_topicProcs.Keys)
			{
				string regexPattern = key;
				if (key.EndsWith("#"))
				{
					regexPattern = regexPattern.Replace("#", ".*");
				}
				if (key.Contains("+"))
				{
					regexPattern = regexPattern.Replace("+", "[a-zA-Z0-9]*");
				}

				Regex regex = new Regex(regexPattern);
				if (regex.IsMatch(topic))
					yield return s_topicProcs[key];
			}

			yield break;
		}

		public void ProcessMessage(MqttApplicationMessageReceivedEventArgs msg)
		{
			if (msg == null || msg.ApplicationMessage == null || msg.ApplicationMessage.Payload == null)
				return;
#if DEBUG
			Console.WriteLine("");
			Console.WriteLine($"Message arrived! Topic:{msg.ApplicationMessage.Topic} Payload:{Encoding.UTF8.GetString(msg.ApplicationMessage.Payload)}");
			Console.WriteLine("");
#endif

			foreach (string fullPath in GetMatchTopicProcs(msg.ApplicationMessage.Topic))
			{
				try
				{
					FileInfo info = new FileInfo(fullPath);
					string className = info.Name.Substring(1).Replace(info.Extension, string.Empty); //it's a main. There's a stub (a) 

					Assembly asm = Assembly.LoadFrom(fullPath);
					Type procType = asm.GetTypes().FirstOrDefault(t => t.FullName.EndsWith(className, StringComparison.InvariantCultureIgnoreCase));

					if (procType == null)
						throw new InvalidDataException("Data type not found");

					var methodInfo = procType.GetMethod("execute", new Type[] { typeof(string), typeof(string), typeof(DateTime) });
					if (methodInfo == null)
						throw new NotImplementedException("Method 'execute' not found");

					var obj = Activator.CreateInstance(procType);
					methodInfo.Invoke(obj, new object[] { msg.ApplicationMessage.Topic, Encoding.UTF8.GetString(msg.ApplicationMessage.Payload), DateTime.Now });
				}
				catch (Exception ex)
				{
					Console.WriteLine($"Error executing the procedure at '{fullPath}'");
					Console.WriteLine(ex.Message);
					Exception inner = ex.InnerException;
					while (inner != null)
					{
						Console.WriteLine(inner.Message);
						inner = inner.InnerException;
					}
				}
			}
		}

		public static MqttStatus Connect(string url, MqttConfig config)
		{
			Guid key = Guid.NewGuid();

			try
			{
#if DEBUG
				IMqttClient client = new MqttFactory().CreateMqttClient(new ConsoleLogger());
#else
				IMqttClient client = new MqttFactory().CreateMqttClient();
#endif

				var b = new MqttClientOptionsBuilder()
					.WithTcpServer(url, config.Port)
					.WithKeepAlivePeriod(TimeSpan.FromSeconds(config.KeepAlive))
					.WithMaximumPacketSize(Convert.ToUInt32(config.BufferSize))
					.WithCommunicationTimeout(TimeSpan.FromSeconds(config.ConnectionTimeout))
					.WithCleanSession(config.CleanSession);

				if (config.SessionExpiryInterval > 0)
					b = b.WithSessionExpiryInterval((uint)config.SessionExpiryInterval);

				if (!string.IsNullOrEmpty(config.ClientId))
					b = b.WithClientId(config.ClientId);

				if (!config.SSLConnection)
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

							Certificates = new List<X509Certificate>() { caCert, cliCert },
							//CertificateValidationHandler = context => true
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
							MqttClientAuthenticateResult reconnectResult = await client.ConnectAsync(b.Build());
							SubscribePreviousConnection(key, reconnectResult);
						}
					}
					catch
					{
						Connections.Remove(key);
						client.Dispose();
					}
				});

				MqttClientAuthenticateResult result = client.ConnectAsync(b.Build()).GetAwaiter().GetResult();

				MqttClient mqtt = new MqttClient(client, config);

				Connections[key] = mqtt;

				SubscribePreviousConnection(key, result);

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
				mqtt.m_mqttClient.DisconnectAsync(new MQTTnet.Client.Disconnecting.MqttClientDisconnectOptions() { ReasonCode = MQTTnet.Client.Disconnecting.MqttClientDisconnectReason.NormalDisconnection, ReasonString = "Disconnected by user" }).GetAwaiter().GetResult();
				mqtt.m_mqttClient.Dispose();
			}
			catch (Exception ex)
			{
				return MqttStatus.Fail(key, ex);
			}

			return MqttStatus.Success(key);
		}

		private static void SubscribePreviousConnection(Guid key, MqttClientAuthenticateResult result)
		{
			if (!result.IsSessionPresent)
				return;

			for (int i = 0; i < result.UserProperties.Count; i++)
			{
				MqttUserProperty prop = result.UserProperties[i];
				if (prop.Name == "subscriptions")
				{
					using (var ms = new MemoryStream(Encoding.Unicode.GetBytes(prop.Value)))
					{
						DataContractJsonSerializer serializer = new DataContractJsonSerializer(typeof(SubscriptionList));
						SubscriptionList list = (SubscriptionList)serializer.ReadObject(ms);
						foreach (PreviousSubscription subscription in list)
						{
							MqttClient client = GetClient(key);
							string gxproc = client.GetProc(subscription.topic);
							if (!string.IsNullOrEmpty(gxproc))
								Subscribe(key, subscription.topic, gxproc, subscription.qos);
						}
					}
				}
			}
		}

		public static MqttStatus Subscribe(Guid key, string topic, string gxproc, int qos)
		{
			try
			{
				if (string.IsNullOrEmpty(gxproc))
					throw new ArgumentNullException(nameof(gxproc), "GeneXus procedure parameter cannot be null");

				string fullPath = gxproc;
				if (!Path.IsPathRooted(gxproc))
				{
					string fileName = $"a{gxproc}.dll";
					string baseDir = !string.IsNullOrEmpty(AppDomain.CurrentDomain.RelativeSearchPath) ? AppDomain.CurrentDomain.RelativeSearchPath : AppDomain.CurrentDomain.BaseDirectory;
					fullPath = Path.Combine(baseDir, fileName);
				}

				if (!File.Exists(fullPath))
					throw new FileNotFoundException($"File not found at {fullPath}", fullPath);

				MqttClient mqtt = GetClient(key);

				if (topic.Contains("*") || topic.Contains("+") || topic.Contains("#"))
					if (!mqtt.m_config.AllowWildcardsInTopicFilters)
						throw new InvalidDataException("Wildcards not allowed for this instance.");

				MqttClientSubscribeOptions options = new MqttClientSubscribeOptions
				{
					//TopicFilters = new List<MqttTopicFilter> { new MqttTopicFilter() { Topic = topic, QualityOfServiceLevel = (MQTTnet.Protocol.MqttQualityOfServiceLevel)Enum.ToObject(typeof(MQTTnet.Protocol.MqttQualityOfServiceLevel), qos) } },
					TopicFilters = new List<TopicFilter> { new TopicFilter() { Topic = topic, QualityOfServiceLevel = (MQTTnet.Protocol.MqttQualityOfServiceLevel)Enum.ToObject(typeof(MQTTnet.Protocol.MqttQualityOfServiceLevel), qos) } },
					UserProperties = new List<MqttUserProperty> { new MqttUserProperty("gxrocedure", $"{{\"gxrocedure\":\"{gxproc}\"}}") }
				};

				if (mqtt.m_mqttClient.ApplicationMessageReceivedHandler == null)
					mqtt.m_mqttClient.UseApplicationMessageReceivedHandler(mqtt.ProcessMessage);

				MqttClientSubscribeResult result = mqtt.m_mqttClient.SubscribeAsync(options).GetAwaiter().GetResult();
				mqtt.AddTopic(topic, fullPath);// GetClient s_topicProcs[GetProcKey(mqtt, topic)] = gxproc;

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
				mqtt.m_mqttClient.UnsubscribeAsync(topic).GetAwaiter().GetResult();
				mqtt.RemoveTopic(topic);
			}
			catch (Exception ex)
			{
				return MqttStatus.Fail(key, ex);
			}

			return MqttStatus.Success(key);
		}

		public static MqttStatus Publish(Guid key, string topic, string payload, int qos, bool retainMessage, int messageExpiryInterval)
		{
			try
			{
				MqttClient mqtt = GetClient(key);
				MqttApplicationMessage msg = new MqttApplicationMessage
				{
					MessageExpiryInterval = messageExpiryInterval > 0 ? (uint)messageExpiryInterval : 0,
					Payload = Encoding.UTF8.GetBytes(payload),
					Topic = topic,
					QualityOfServiceLevel = (MQTTnet.Protocol.MqttQualityOfServiceLevel)Enum.ToObject(typeof(MQTTnet.Protocol.MqttQualityOfServiceLevel), qos),
					Retain = retainMessage
				};
				mqtt.m_mqttClient.PublishAsync(msg).GetAwaiter().GetResult();
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
