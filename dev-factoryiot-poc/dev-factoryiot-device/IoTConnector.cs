using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Azure.Devices.Client;
using Microsoft.Azure.Devices.Shared;
using Newtonsoft.Json;
using System.IO;
using System.Configuration;
using System.Security;
using System.Threading.Tasks;
using System.Threading;

namespace dev_factoryiot_device
{
    class IoTConnector
    {
        private DeviceClient deviceClient;
        private FactorySettings factorySetting; 
        private string deviceId = ConfigurationManager.AppSettings["device_id"];
        public enum ConnectionType { X509, ConnectionString }

        public IoTConnector(SecureString password, ConnectionType connectionType, TransportType transportType)
        {

            // Create Device Client
            switch (connectionType)
            {
                case ConnectionType.X509:
                    ConnectViaX509(password, transportType);
                    break;
                case ConnectionType.ConnectionString:
                    ConnectViaConnectionString(transportType);
                    break;
            }

            // Check if it worked
            if (deviceClient == null)
            {
                Console.WriteLine("Failed to create DeviceClient!");
            }
            else
            {
                // Create Settings
                factorySetting = new FactorySettings(deviceClient);

                Console.WriteLine($"Successfully created DeviceClient for device {deviceId}!");

                var cts = new CancellationTokenSource();

                var result = SyncFactoryProperties();
                result.Wait();

                SendDeviceToCloudMessagesAsync(cts.Token);
                ReceiveCloudToDeviceMessagesAsync(cts.Token);

                deviceClient.SetDesiredPropertyUpdateCallbackAsync(HandleSettingChanged, null).Wait();
                Console.ReadKey();
                cts.Cancel();
            }

        }

        private void ConnectViaConnectionString(TransportType transportType)
        {
            string connectionString = ConfigurationManager.AppSettings["device_conn_str"];
            deviceClient = DeviceClient.CreateFromConnectionString(connectionString, transportType);
        }

        private void ConnectViaX509(SecureString password, TransportType transportType)
        {
            string certPath = ConfigurationManager.AppSettings["cert_path"];
            string iotHubUri = ConfigurationManager.AppSettings["iot_hub"];

            System.Security.Cryptography.X509Certificates.X509Certificate2 myCert = new System.Security.Cryptography.X509Certificates.X509Certificate2(certPath, password);

            deviceClient = DeviceClient.Create(iotHubUri, new DeviceAuthenticationWithX509Certificate(deviceId, myCert), transportType);
        }

        public async Task SyncFactoryProperties()
        {
            try
            {
                Console.WriteLine("Sending factory properties:");
                Console.WriteLine(JsonConvert.SerializeObject(factorySetting));

                var twin = await deviceClient.GetTwinAsync();

                await HandleSettingChanged(new TwinCollection(JsonConvert.SerializeObject(twin.Properties.Desired)), null);

                await deviceClient.UpdateReportedPropertiesAsync(factorySetting);
            }
            catch (Exception ex)
            {
                Console.WriteLine();
                Console.WriteLine("Error in factory properties: {0}", ex.Message);
            }
        }


        private async void ReceiveCloudToDeviceMessagesAsync(CancellationToken token)
        {
            while (!token.IsCancellationRequested)
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

                await Task.Delay(factorySetting.ReadIntervalInMs);
            }
        }

        private async void SendDeviceToCloudMessagesAsync(CancellationToken token)
        {   
            Random rand = new Random();
            double currentTemperature = factorySetting.Temperature;

            while (!token.IsCancellationRequested)
            {
                int producedUnits = 0;

                if (factorySetting.Overheated)
                {
                    //Overheat
                    currentTemperature = currentTemperature - ((factorySetting.CooldownPerMinute / 60) * (factorySetting.SendIntervalInMs / 1000));
                    if (factorySetting.RestartCooldownTemp > currentTemperature)
                    {
                        factorySetting.Overheated = false;
                    }
                }
                else if (factorySetting.Activated)
                {
                    //Normal progress
                    producedUnits = Convert.ToInt32((factorySetting.UnitPerMinute / 60) * (factorySetting.SendIntervalInMs / 1000));
                    currentTemperature = (currentTemperature + rand.NextDouble() * factorySetting.HeatPerUnit * (factorySetting.UnitPerMinute / 60) * (factorySetting.SendIntervalInMs / 1000)) 
                        - ((factorySetting.CooldownPerMinute / 60 ) * (factorySetting.SendIntervalInMs / 1000));
                    factorySetting.Overheated = (currentTemperature > factorySetting.OverheatLimit);
                }
                
                // Create Message
                var telemetryDataPoint = new
                {
                    deviceId,
                    temperature = currentTemperature,
                    newUnits = producedUnits
                };

                // Serialize and send but only if device is activated
                if (factorySetting.Activated)
                { 
                    var messageString = JsonConvert.SerializeObject(telemetryDataPoint);
                    var message = new Message(Encoding.UTF8.GetBytes(messageString));

                    await deviceClient.SendEventAsync(message);

                    Console.WriteLine("{0} > Sending message: {1}", DateTime.Now, messageString);
                }

                await Task.Delay(factorySetting.SendIntervalInMs);
            }
        }

        private async Task HandleSettingChanged(TwinCollection desiredProperties, object userContext)
        {
            try
            {
                Console.WriteLine("Received settings change...");
                Console.WriteLine(JsonConvert.SerializeObject(desiredProperties));

                foreach(var item in factorySetting.UpdatableSettings())
                {
                    SetSetting(desiredProperties, item);
                }
                
                //SetSetting(desiredProperties, "UnitPerMinute");
                //SetSetting(desiredProperties, "SendIntervalInMs");
                //SetSetting(desiredProperties, "ReadIntervalInMs");

                await deviceClient.UpdateReportedPropertiesAsync(factorySetting);
            }

            catch (Exception ex)
            {
                Console.WriteLine();
                Console.WriteLine("Error in sample: {0}", ex.Message);
            }
        }

        private void SetSetting(TwinCollection desiredProperties, string setting)
        {
            if (desiredProperties.Contains(setting))
            {
                // Act on setting change, then
                AcknowledgeSettingChange(desiredProperties, setting);
            }
        }

        private void AcknowledgeSettingChange(TwinCollection desiredProperties, string setting)
        {
            var value = new
            {
                value = desiredProperties[setting]["value"],
                status = "completed",
                desiredVersion = desiredProperties["$version"],
                message = "Processed"
            };

            factorySetting.UpdateDeviceProperties(setting, value);
        }
    }

}
