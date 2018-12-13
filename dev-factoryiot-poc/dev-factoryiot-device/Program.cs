using System;
using System.Text;

namespace dev_factoryiot_device
{
    class Program
    {
        static void Main(string[] args)
        {
            var switchOption = args.Length < 1 ? "DPS" : args[0];

            var pass = new StringBuilder();
            pass.Append(Environment.GetEnvironmentVariable("PASSWORD"));
            if (pass.Length == 0) pass.Append(System.Configuration.ConfigurationManager.AppSettings["PASSWORD"]);

            var securePassword = new System.Security.SecureString();
            if (pass.Length == 0)
            {
                securePassword = Helper.FromConsole();
            }
            else
            {
                foreach (char c in pass.ToString())
                {
                    securePassword.AppendChar(c);
                }
            }

            var iothuburl = new StringBuilder();
            iothuburl.Append(Environment.GetEnvironmentVariable("IOT_HUB_URI"));
            if (iothuburl.Length == 0) iothuburl.Append(System.Configuration.ConfigurationManager.AppSettings["IOT_HUB_URI"]);

            switch (switchOption)
            {
                case "IoTHub":
                    new IoTConnector(securePassword, IoTConnector.ConnectionType.X509, Microsoft.Azure.Devices.Client.TransportType.Mqtt_Tcp_Only).Start();
                    break;
                case "IoTCentral":
                    new IoTConnector(new System.Security.SecureString(), IoTConnector.ConnectionType.ConnectionString, Microsoft.Azure.Devices.Client.TransportType.Mqtt_Tcp_Only).Start();
                    break;
                case "Both":
                    var password = securePassword;
                    var iotConnector = new IoTConnector(new System.Security.SecureString(), IoTConnector.ConnectionType.ConnectionString, Microsoft.Azure.Devices.Client.TransportType.Mqtt_Tcp_Only);
                    iotConnector.AddSecondaryConnection(password, IoTConnector.ConnectionType.X509, Microsoft.Azure.Devices.Client.TransportType.Mqtt_Tcp_Only);
                    iotConnector.Start();
                    break;
                case "DPS":
                    var iotConnector2 = new IoTConnector(securePassword, IoTConnector.ConnectionType.DPSX509, Microsoft.Azure.Devices.Client.TransportType.Mqtt_Tcp_Only);
                    if (iothuburl.Length != 0)
                    {
                        iotConnector2.AddSecondaryConnection(securePassword, IoTConnector.ConnectionType.X509, Microsoft.Azure.Devices.Client.TransportType.Mqtt_Tcp_Only);
                    }
                    iotConnector2.Start();
                    break;
                default:
                    new IoTConnector(securePassword, IoTConnector.ConnectionType.DPSX509, Microsoft.Azure.Devices.Client.TransportType.Mqtt_Tcp_Only).Start();
                    break;
            }
        }
    }
}
