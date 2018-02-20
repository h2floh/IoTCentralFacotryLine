using System;

namespace dev_factoryiot_device
{
    class Program
    {
        static void Main(string[] args)
        {
            new IoTConnector(Helper.FromConsole());

            Console.ReadLine();
        }
    }
}
