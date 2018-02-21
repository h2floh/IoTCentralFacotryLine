using System;
using System.Collections.Generic;
using Microsoft.Azure.Devices.Shared;
using System.Configuration;
using System.Text;
using Microsoft.Azure.Devices.Client;
using Newtonsoft.Json;

namespace dev_factoryiot_device
{
    class FactorySettings : TwinCollection
    {
        public Double Temperature { get; set; } //excluded from sending property to the device twin
        public Double UnitPerMinute { get { return this["UnitPerMinute"].value; } set { UpdateDeviceProperties("UnitPerMinute", value); } }
        public Double HeatPerUnit { get { return this["HeatPerUnit"].value; } set { UpdateDeviceProperties("HeatPerUnit", value); } }
        public Double OverheatLimit { get { return this["OverheatLimit"].value; } set { UpdateDeviceProperties("OverheatLimit", value); } }
        public Double CooldownPerMinute { get { return this["CooldownPerMinute"].value; } set { UpdateDeviceProperties("CooldownPerMinute", value); } }
        public Double RestartCooldownTemp { get { return this["RestartCooldownTemp"].value; } set { UpdateDeviceProperties("RestartCooldownTemp", value); } }
        public int SendIntervalInMs { get { return this["SendIntervalInMs"].value; } set { UpdateDeviceProperties("SendIntervalInMs", value); } }
        public int ReadIntervalInMs { get { return this["ReadIntervalInMs"].value; } set { UpdateDeviceProperties("ReadIntervalInMs", value); } }
        public bool Overheated { get { return this["Overheated"].value; } set { UpdateDeviceProperties("Overheated", value); } }
        public bool Activated { get { return this["Activated"].value; } set { UpdateDeviceProperties("Activated", value); } }

        private DeviceClient deviceClient;

        public FactorySettings(DeviceClient deviceClient) : base()
        {
            this.deviceClient = deviceClient;

            Temperature = 20;
            UpdateDeviceProperties("UnitPerMinute", 60, false);
            UpdateDeviceProperties("HeatPerUnit", 5, false);
            UpdateDeviceProperties("OverheatLimit", 200, false);
            UpdateDeviceProperties("CooldownPerMinute", 20, false);
            UpdateDeviceProperties("RestartCooldownTemp", 100, false);
            UpdateDeviceProperties("SendIntervalInMs", 10000, false);
            UpdateDeviceProperties("ReadIntervalInMs", 1000, false);
            UpdateDeviceProperties("Overheated", false, false);
            UpdateDeviceProperties("Activated", true, false);

            //this["UnitPerMinute"] = 60;
            //this["HeatPerUnit"] = 5;
            //this["OverheatLimit"] = 200;
            //this["CooldownPerMinute"] = 20;
            //this["RestartCooldownTemp"] = 100;
            //this["SendIntervalInMs"] = 10000;
            //this["ReadIntervalInMs"] = 1000;
            //this["Overheated"] = false;
            //this["Activated"] = true;
        }

        public void UpdateDeviceProperties(string setting, dynamic value, bool updateToCloud = true)
        {
            if (value is Double || value is int || value is bool )
            {
                if (!this.Contains(setting) || this[setting].value != value)
                {
                    this[setting] = new
                    {
                        value
                    };

                    if (updateToCloud)
                    {
                        UpdateDevicePropertiesToCloud();
                    }
                }
            }
            else
            {
                if (!this.Contains(setting) || JsonConvert.SerializeObject(this[setting]) != JsonConvert.SerializeObject(value))
                {
                    this[setting] = value;

                    if (updateToCloud)
                    {
                        UpdateDevicePropertiesToCloud();
                    }
                }
            }
        }

        private async void UpdateDevicePropertiesToCloud()
        {
            try
            {
                Console.WriteLine("Sending factory properties:");
                Console.WriteLine(JsonConvert.SerializeObject(this));

                await deviceClient.UpdateReportedPropertiesAsync(this);
            }
            catch (Exception ex)
            {
                Console.WriteLine();
                Console.WriteLine("Error in factory properties: {0}", ex.Message);
            }
        }
    }
}
