namespace MQTTLib
{
    public class MqttConfig
    {
        public int Port { get; set; } = 1883;
        public int BufferSize { get; set; } = 8192;
        public int KeepAlive { get; set; } = 0;
        public int WaitTimeout { get; set; } = 5;
        public int ConnectionTimeout { get; set; } = 5000;
        public int ReconnectPeriod { get; set; } = 1000;
        public string UserName { get; set; }
        public string Password { get; set; }
        public string MQTTConnectionName { get; set; } = "mqtt_connection1";
        public string ClientId { get; set; }
        public bool SSLConnection { get; set; }


        public static MqttConfig Default
        {
            get { return new MqttConfig(); }
        }
    }
}