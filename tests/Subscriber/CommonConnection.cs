using System;
using System.Configuration;
using MQTTLib;

namespace Common
{
    internal class CommonConnection
    {
        public static Guid ConnectTLS()
        {
            MqttConfig config = GetConfig();

            config.CertificateKey = @"MIIGzDCCBbSgAwIBAgIMPFEC2JrizEiAhCVqMA0GCSqGSIb3DQEBCwUAMEwxCzAJ
BgNVBAYTAkJFMRkwFwYDVQQKExBHbG9iYWxTaWduIG52LXNhMSIwIAYDVQQDExlB
bHBoYVNTTCBDQSAtIFNIQTI1NiAtIEcyMB4XDTE3MTIxMTEyMjgzN1oXDTIwMTIx
MTEyMjgzN1owOTEhMB8GA1UECxMYRG9tYWluIENvbnRyb2wgVmFsaWRhdGVkMRQw
EgYDVQQDDAsqLmZsZXNwaS5pbzCCASIwDQYJKoZIhvcNAQEBBQADggEPADCCAQoC
ggEBAMVsit1Mh2Ytyym51hFaMcryuiJxl56TFCaeBeppimNw9Fc43xFrWd9yyEp3
BuWHjd29srtOBrThImXoCfbcSy7NpzlOSJlShhBfpyNiodE6N6DkHOisUibhWwj2
otR5VYmP7AQPzpfaKhM0CRcAB733X5Yd/stvbCVQB9069Zyd+lHteL+zKR73muFA
DYZp8angPB9m6fJwbW6LMsFjt19cB31HrDbFg8VXxkgJ/7F988dyHcuReEJTyI+C
XbW9vFyF+/RYm0Gjha3FZcXEV+W1VwP/ZIjiZzlYCxUM72yr96Pfw2X3HG+D3v0t
/uOoCZGXUm7hJtjPXirhFwV0o7UCAwEAAaOCA78wggO7MA4GA1UdDwEB/wQEAwIF
oDCBiQYIKwYBBQUHAQEEfTB7MEIGCCsGAQUFBzAChjZodHRwOi8vc2VjdXJlMi5h
bHBoYXNzbC5jb20vY2FjZXJ0L2dzYWxwaGFzaGEyZzJyMS5jcnQwNQYIKwYBBQUH
MAGGKWh0dHA6Ly9vY3NwMi5nbG9iYWxzaWduLmNvbS9nc2FscGhhc2hhMmcyMFcG
A1UdIARQME4wQgYKKwYBBAGgMgEKCjA0MDIGCCsGAQUFBwIBFiZodHRwczovL3d3
dy5nbG9iYWxzaWduLmNvbS9yZXBvc2l0b3J5LzAIBgZngQwBAgEwCQYDVR0TBAIw
ADA+BgNVHR8ENzA1MDOgMaAvhi1odHRwOi8vY3JsMi5hbHBoYXNzbC5jb20vZ3Mv
Z3NhbHBoYXNoYTJnMi5jcmwwIQYDVR0RBBowGIILKi5mbGVzcGkuaW+CCWZsZXNw
aS5pbzAdBgNVHSUEFjAUBggrBgEFBQcDAQYIKwYBBQUHAwIwHQYDVR0OBBYEFOKx
KLsJ/+eKGfPhv553Hwwh43cMMB8GA1UdIwQYMBaAFPXN1TwIUPlqTzq3l9pWg+Zp
0mj3MIIB9QYKKwYBBAHWeQIEAgSCAeUEggHhAd8AdgDd6x0reg1PpiCLga2BaHB+
Lo6dAdVciI09EcTNtuy+zAAAAWBFjG/AAAAEAwBHMEUCIQDRPNudq8fp8HvpMFDN
pvxiMRhYqgVTUhTYr7KJc30dBQIgZRgSp3ytL9kfGtJmGDfXYp6ffyZO1JJnbSd3
BF2GSC4AdQBWFAaaL9fC7NP14b1Esj7HRna5vJkRXMDvlJhV1onQ3QAAAWBFjHAY
AAAEAwBGMEQCICIqW+E4s6O2FbiNqh7li0kMLo1zOKrfmEitnSKIx2DbAiBhtxsd
3e97ravHoGAW3yXRF3jwezfEwyRQ3xXHA98BNAB2AKS5CZC0GFgUh7sTosxncAo8
NZgE+RvfuON3zQ7IDdwQAAABYEWMcpQAAAQDAEcwRQIgJ0EiJuYsYOeyziLXrN+C
dFmaDNyPGbLFWm5VqCi1OpcCIQDTM7TyakLFmBXrEkeJXuOH+ECJ2l4ZVIcoGrdM
UahOCgB2ALvZ37wfinG1k5Qjl6qSe0c4V5UKq1LoGpCWZDaOHtGFAAABYEWMc0wA
AAQDAEcwRQIhAN5+7KD0MrK4d7rbkwHf9fZh0yERoD4F+eEijXiqCruTAiBpIHOb
eEK5xp1IQV7dZQw/eknxyGffqHI6ctYNDpe5DjANBgkqhkiG9w0BAQsFAAOCAQEA
U9yjiRzpDp+Alz68X4K7EbyrGJQJRma5Bkm0IaCwv2gNnOsqRVQPMLz69ft42SbK
ECMGipkiz9VilRmMX82TGuKLuxgZEfYkWr31A7EYjzZ+iFyYMilbI+DA+pi7zcAA
a9wa2D9DT7IC0e9pv1gqiWjA92KJcy+LZt9+xeQCpmdBefxIRZ2g1oKXrUSnlfCt
jZ153YD11Lqrq5ZMS2fedkzNHmasKYJdipXoXcpTywGn6QEDFe22V+HCdDsLWw3d
1MHKVCc+vNcDG0FkrItWjB857pwdZN36VqVVvGuio4oeGOmTHMNzLahN6mA4tABN
Ht7LDqFUSy2ZL4yTyCqEnA==";

            config.ClientKeyPassphrase = @"";

            config.SSLConnection = true;
            config.Port = 8883;

            return Connect(config);
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
