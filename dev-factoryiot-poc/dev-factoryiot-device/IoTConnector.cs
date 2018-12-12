using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Azure.Devices.Client;
using Microsoft.Azure.Devices.Shared;
using Microsoft.Azure.Devices.Provisioning.Client;
using Microsoft.Azure.Devices.Provisioning.Client.Transport;
using Newtonsoft.Json;
using System.IO;
using System.Configuration;
using System.Security;
using System.Threading.Tasks;
using System.Threading;
using System.Text.RegularExpressions;

namespace dev_factoryiot_device
{
    class IoTConnector
    {
        private const string GlobalDeviceEndpoint = "global.azure-devices-provisioning.net";
        private DeviceClient deviceClient;
        private DeviceClient secondaryDeviceClient;
        private FactorySettings factorySetting; 
        private string deviceId = ConfigurationManager.AppSettings["device_id"];
        public enum ConnectionType { X509, ConnectionString, DPSX509 }
        private enum ClientType { Primary, Secondary }

        public IoTConnector(SecureString password, ConnectionType connectionType, TransportType transportType)
        {
            // Add primary connection
            AddConnection(password, ClientType.Primary, connectionType, transportType);
        }

        public void Start()
        {
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

        private void AddConnection(SecureString password, ClientType clientType, ConnectionType connectionType, TransportType transportType)
        {
            // Create Device Client
            switch (connectionType)
            {
                case ConnectionType.X509:
                    ConnectViaX509(password, clientType, transportType);
                    break;
                case ConnectionType.ConnectionString:
                    ConnectViaConnectionString(clientType, transportType);
                    break;
                case ConnectionType.DPSX509:
                    ConnectViaDPSX509(password, transportType);
                    break;
            }
        }

        public void AddSecondaryConnection(SecureString password, ConnectionType connectionType, TransportType transportType)
        {
            // Add secondary connection
            AddConnection(password, ClientType.Secondary, connectionType, transportType);
        }


        private void ConnectViaConnectionString(ClientType clientType, TransportType transportType)
        {
            string connectionString = ConfigurationManager.AppSettings["device_conn_str"];
            // Device Client Type
            switch (clientType)
            {
                case ClientType.Primary:
                    deviceClient = DeviceClient.CreateFromConnectionString(connectionString, transportType);
                    break;
                case ClientType.Secondary:
                    secondaryDeviceClient = DeviceClient.CreateFromConnectionString(connectionString, transportType);
                    break;
            }
        }

        private void ConnectViaX509(SecureString password, ClientType clientType, TransportType transportType)
        {
            string certPath = ConfigurationManager.AppSettings["cert_path"];
            string iotHubUri = ConfigurationManager.AppSettings["iot_hub"];

            System.Security.Cryptography.X509Certificates.X509Certificate2 myCert = new System.Security.Cryptography.X509Certificates.X509Certificate2(certPath, password);

            // Device Client Type
            switch (clientType)
            {
                case ClientType.Primary:
                    deviceClient = DeviceClient.Create(iotHubUri, new DeviceAuthenticationWithX509Certificate(deviceId, myCert), transportType);
                    break;
                case ClientType.Secondary:
                    secondaryDeviceClient = DeviceClient.Create(iotHubUri, new DeviceAuthenticationWithX509Certificate(deviceId, myCert), transportType);
                    break;
            }
        }

        private void ConnectViaDPSX509(SecureString password, TransportType transportType)
        {
            string certPath = Environment.GetEnvironmentVariable("DEVICE_CERTIFICATE");
            if (certPath == null) certPath = ConfigurationManager.AppSettings["DEVICE_CERTIFICATE"];

            string scopeId = Environment.GetEnvironmentVariable("DPS_IDSCOPE");
            if (scopeId == null) scopeId = ConfigurationManager.AppSettings["DPS_IDSCOPE"];

            System.Security.Cryptography.X509Certificates.X509Certificate2 myCert = 
                new System.Security.Cryptography.X509Certificates.X509Certificate2(certPath, password);

            using (var security = new SecurityProviderX509Certificate(myCert)) { 

                using (var transport = new ProvisioningTransportHandlerMqtt(TransportFallbackType.TcpOnly))
                // using (var transport = new ProvisioningTransportHandlerHttp())
                // using (var transport = new ProvisioningTransportHandlerMqtt(TransportFallbackType.TcpOnly))
                // using (var transport = new ProvisioningTransportHandlerMqtt(TransportFallbackType.WebSocketOnly))
                {
                    ProvisioningDeviceClient provClient =
                        ProvisioningDeviceClient.Create(GlobalDeviceEndpoint, scopeId, security, transport);

                    Console.WriteLine($"RegistrationID = {security.GetRegistrationID()}");
                    VerifyRegistrationIdFormat(security.GetRegistrationID());

                    Console.Write("ProvisioningClient RegisterAsync . . . ");
                    DeviceRegistrationResult result = provClient.RegisterAsync().Result;

                    Console.WriteLine($"{result.Status}");
                    Console.WriteLine($"ProvisioningClient AssignedHub: {result.AssignedHub}; DeviceID: {result.DeviceId}");

                    if (result.Status != ProvisioningRegistrationStatusType.Assigned) return;

                    var auth = new DeviceAuthenticationWithX509Certificate(result.DeviceId, (security as SecurityProviderX509).GetAuthenticationCertificate());

                    deviceClient = DeviceClient.Create(result.AssignedHub, auth, transportType);
                }

            }

        }

        private void VerifyRegistrationIdFormat(string v)
        {
            var r = new Regex("^[a-z0-9-]*$");
            if (!r.IsMatch(v))
            {
                throw new FormatException("Invalid registrationId: The registration ID is alphanumeric, lowercase, and may contain hyphens");
            }
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

        private double CheckBaseTemp(double temp)
        {
            return (temp < 20) ? 20 : temp;
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
                    currentTemperature = CheckBaseTemp(
                        currentTemperature - ((factorySetting.CooldownPerMinute / 60) * (factorySetting.SendIntervalInMs / 1000))
                        );
                    
                    if (factorySetting.RestartCooldownTemp > currentTemperature)
                    {
                        factorySetting.Overheated = false;
                    }
                }
                else if (factorySetting.Activated)
                {
                    //Normal progress
                    producedUnits = Convert.ToInt32((factorySetting.UnitPerMinute / 60) * (factorySetting.SendIntervalInMs / 1000));
                    currentTemperature = CheckBaseTemp(
                        (currentTemperature + rand.NextDouble() * factorySetting.HeatPerUnit * (factorySetting.UnitPerMinute / 60) * (factorySetting.SendIntervalInMs / 1000)) 
                        - ((factorySetting.CooldownPerMinute / 60 ) * (factorySetting.SendIntervalInMs / 1000))
                        );
                    factorySetting.Overheated = (currentTemperature > factorySetting.OverheatLimit);
                }

                
                
                // Create Message
                var telemetryDataPoint = new
                {
                    deviceId,
                    temperature = currentTemperature,
                    newUnits = producedUnits,
                    overheated = factorySetting.Overheated
                };

                // Serialize and send but only if device is activated
                if (factorySetting.Activated)
                { 
                    var messageString = JsonConvert.SerializeObject(telemetryDataPoint);
                    var message = new Message(Encoding.UTF8.GetBytes(messageString));

                    await deviceClient.SendEventAsync(message);

                    if (secondaryDeviceClient != null)
                    {
                        message.BodyStream.Position = 0;
                        await secondaryDeviceClient.SendEventAsync(message);
                    }

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
