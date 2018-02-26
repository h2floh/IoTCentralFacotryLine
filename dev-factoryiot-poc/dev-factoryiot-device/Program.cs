using System;

namespace dev_factoryiot_device
{
    class Program
    {
        static void Main(string[] args)
        {
            var switchOption = args.Length < 1 ? "Both" : args[0];

            switch (switchOption)
            {
                case "IoTHub":
                    new IoTConnector(Helper.FromConsole(), IoTConnector.ConnectionType.X509, Microsoft.Azure.Devices.Client.TransportType.Mqtt_Tcp_Only).Start();
                    break;
                case "IoTCentral":
                    new IoTConnector(new System.Security.SecureString(), IoTConnector.ConnectionType.ConnectionString, Microsoft.Azure.Devices.Client.TransportType.Mqtt_Tcp_Only).Start();
                    break;
                case "Both":
                    var password = Helper.FromConsole();
                    var iotConnector = new IoTConnector(new System.Security.SecureString(), IoTConnector.ConnectionType.ConnectionString, Microsoft.Azure.Devices.Client.TransportType.Mqtt_Tcp_Only);
                    iotConnector.AddSecondaryConnection(password, IoTConnector.ConnectionType.X509, Microsoft.Azure.Devices.Client.TransportType.Mqtt_Tcp_Only);
                    iotConnector.Start();
                    break;
                default:
                    new IoTConnector(Helper.FromConsole(), IoTConnector.ConnectionType.X509, Microsoft.Azure.Devices.Client.TransportType.Mqtt_Tcp_Only).Start();
                    break;
            }

            Console.ReadLine();
        }
    }
}
