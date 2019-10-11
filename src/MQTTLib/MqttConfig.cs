using MQTTnet.Formatter;
using System.IO;
using System.Runtime.Serialization.Json;
using System.Text;

namespace MQTTLib
{
    public class MqttConfig
    {
        public int Port { get; set; } = 1883;
        public int BufferSize { get; set; } = 8192;
        public int KeepAlive { get; set; } = 0;
        public int WaitTimeout { get; set; } = 5;
        //public int ConnectionTimeout { get; set; } = 5000;
        //public int ReconnectPeriod { get; set; } = 1000;
        public string UserName { get; set; }
        public string Password { get; set; }
        public string MQTTConnectionName { get; set; } = "mqtt_connection1";
        public string ClientId { get; set; }
        public bool SSLConnection { get; set; }
        public string CAcertificateKey { get; set; }
        public string CertificateKey { get; set; }
        public string PrivateKey { get; set; }
        public string ClientKeyPassphrase { get; set; }
		public int ProtocolVersion { get; set; } = 500;


		public static MqttConfig Default
        {
            get { return new MqttConfig(); }
        }

        public string ExportMqttConfig()
        {
            byte[] json;
            using (var ms = new MemoryStream())
            {
                var ser = new DataContractJsonSerializer(typeof(MqttConfig));
                ser.WriteObject(ms, this);
                json = ms.ToArray();
            }
            return Encoding.UTF8.GetString(json);
        }

        public static MqttConfig ImportMqttConfig(string json)
        {
            var config = new MqttConfig();
            using (var ms = new MemoryStream(Encoding.UTF8.GetBytes(json)))
            {
                var ser = new DataContractJsonSerializer(typeof(MqttConfig));
                config = ser.ReadObject(ms) as MqttConfig;
            }

            return config;
        }
    }
}
