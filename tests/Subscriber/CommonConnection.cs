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
                Port = 8884
            };

            config.CAcertificateKey = @"MIIC8DCCAlmgAwIBAgIJAOD63PlXjJi8MA0GCSqGSIb3DQEBBQUAMIGQMQswCQYD
VQQGEwJHQjEXMBUGA1UECAwOVW5pdGVkIEtpbmdkb20xDjAMBgNVBAcMBURlcmJ5
MRIwEAYDVQQKDAlNb3NxdWl0dG8xCzAJBgNVBAsMAkNBMRYwFAYDVQQDDA1tb3Nx
dWl0dG8ub3JnMR8wHQYJKoZIhvcNAQkBFhByb2dlckBhdGNob28ub3JnMB4XDTEy
MDYyOTIyMTE1OVoXDTIyMDYyNzIyMTE1OVowgZAxCzAJBgNVBAYTAkdCMRcwFQYD
VQQIDA5Vbml0ZWQgS2luZ2RvbTEOMAwGA1UEBwwFRGVyYnkxEjAQBgNVBAoMCU1v
c3F1aXR0bzELMAkGA1UECwwCQ0ExFjAUBgNVBAMMDW1vc3F1aXR0by5vcmcxHzAd
BgkqhkiG9w0BCQEWEHJvZ2VyQGF0Y2hvby5vcmcwgZ8wDQYJKoZIhvcNAQEBBQAD
gY0AMIGJAoGBAMYkLmX7SqOT/jJCZoQ1NWdCrr/pq47m3xxyXcI+FLEmwbE3R9vM
rE6sRbP2S89pfrCt7iuITXPKycpUcIU0mtcT1OqxGBV2lb6RaOT2gC5pxyGaFJ+h
A+GIbdYKO3JprPxSBoRponZJvDGEZuM3N7p3S/lRoi7G5wG5mvUmaE5RAgMBAAGj
UDBOMB0GA1UdDgQWBBTad2QneVztIPQzRRGj6ZHKqJTv5jAfBgNVHSMEGDAWgBTa
d2QneVztIPQzRRGj6ZHKqJTv5jAMBgNVHRMEBTADAQH/MA0GCSqGSIb3DQEBBQUA
A4GBAAqw1rK4NlRUCUBLhEFUQasjP7xfFqlVbE2cRy0Rs4o3KS0JwzQVBwG85xge
REyPOFdGdhBY2P1FNRy0MDr6xr+D2ZOwxs63dG1nnAnWZg7qwoLgpZ4fESPD3PkA
1ZgKJc2zbSQ9fCPxt2W3mdVav66c6fsb7els2W2Iz7gERJSX";

            config.PrivateKey = @"";

            config.ClientCertificatePath = @"C:\code\genexus\MQTT\tests\OpenSSL\cli.pfx";


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
