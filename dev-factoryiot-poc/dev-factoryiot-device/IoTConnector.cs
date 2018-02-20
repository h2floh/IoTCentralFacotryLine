using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Azure.Devices.Client;
using Microsoft.Azure.Devices;
using Newtonsoft.Json;
using System.IO;
using System.Configuration;
using System.Security;
using System.Threading.Tasks;

namespace dev_factoryiot_device
{
    class IoTConnector
    {
        private DeviceClient deviceClient;
        private double _temperature = 20;
        private double _productionUnitPerMinute = 60;
        private double _heatPerUnit = 5;
        private double _overheatLimit = 200;
        private double _cooldownPerMinute = 20;
        private double _restartCooldownTemp = 100;
        private int _sendIntervalInMs = 10000;
        private int _readIntervalInMs = 1000;
        private bool _overheated = false;
        private int messageId = 1;
        private string deviceId = ConfigurationManager.AppSettings["device_id"];

        public IoTConnector(SecureString password)
        {
            string certPath = ConfigurationManager.AppSettings["cert_path"];
            string iotHubUri = ConfigurationManager.AppSettings["iot_hub"];

            System.Security.Cryptography.X509Certificates.X509Certificate2 myCert = new System.Security.Cryptography.X509Certificates.X509Certificate2(certPath, password);

            deviceClient = DeviceClient.Create(iotHubUri, new DeviceAuthenticationWithX509Certificate(deviceId, myCert), TransportType.Mqtt);

            if (deviceClient == null)
            {
                Console.WriteLine("Failed to create DeviceClient!");
            }
            else
            {
                Console.WriteLine($"Successfully created DeviceClient for device {deviceId}!");
                SendDeviceToCloudMessagesAsync();
                ReceiveCloudToDeviceMessagesAsync();
            }

        }

        private async void ReceiveCloudToDeviceMessagesAsync()
        {
            while (true)
            {
                Task<Message> message;

                message = deviceClient.ReceiveAsync();

                await message;

                try { 
                    var reader = new StreamReader(message.Result.GetBodyStream(), Encoding.UTF8);

                    Console.WriteLine("{0} > Receiving message: {1}", DateTime.Now, reader.ReadToEnd());
                } catch
                {
                    Console.WriteLine("{0} > Receiving Message TimeOut", DateTime.Now);
                }

                await Task.Delay(_readIntervalInMs);
            }
        }

        private async void SendDeviceToCloudMessagesAsync()
        {   
            Random rand = new Random();
            double currentTemperature = _temperature;

            while (true)
            {
                int producedUnits;

                if (_overheated)
                {
                    //Overheat
                    producedUnits = 0;
                    currentTemperature = currentTemperature - (_cooldownPerMinute / (_sendIntervalInMs / 1000));
                    if (_restartCooldownTemp > currentTemperature)
                    {
                        _overheated = false;
                    }
                }
                else
                {
                    //Normal progress
                    producedUnits = Convert.ToInt32((_productionUnitPerMinute / 60) * (_sendIntervalInMs / 1000));
                    currentTemperature = currentTemperature + rand.NextDouble() * _heatPerUnit * (_productionUnitPerMinute / 60) * (_sendIntervalInMs / 1000) - (_cooldownPerMinute / (_sendIntervalInMs / 1000));
                    _overheated = (currentTemperature > _overheatLimit);
                }
                
                // Create Message
                var telemetryDataPoint = new
                {
                    messageId = messageId++,
                    deviceId,
                    temperature = currentTemperature,
                    newUnits = producedUnits
                };

                // Serialize and send
                var messageString = JsonConvert.SerializeObject(telemetryDataPoint);
                var message = new Message(Encoding.UTF8.GetBytes(messageString));
                message.Properties.Add("overheat", _overheated ? "true" : "false");

                await deviceClient.SendEventAsync(message);

                Console.WriteLine("{0} > Sending message: {1}", DateTime.Now, messageString);

                await Task.Delay(_sendIntervalInMs);
            }

        }
    }
}
