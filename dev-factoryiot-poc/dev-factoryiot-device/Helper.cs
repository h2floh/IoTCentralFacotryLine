using System;
using System.Collections.Generic;
using System.Text;
using System.Security;

namespace dev_factoryiot_device
{
    public class Helper
    {
        public static SecureString FromConsole()
        {
            Console.Write("Please enter certificate private key password: ");

            ConsoleKeyInfo s = new ConsoleKeyInfo();
            SecureString secure = new SecureString();

            s = Console.ReadKey(true);
            while (s.Key != ConsoleKey.Enter)
            {
                secure.AppendChar(s.KeyChar);
                s = Console.ReadKey(true);
            }
            Console.WriteLine();
            return secure;
        }

        public static SecureString ToSecureString(string input)
        {
            SecureString secure = new SecureString();
            foreach (char c in input)
            {
                secure.AppendChar(c);
            }

            secure.MakeReadOnly();
            input = "[Guid(1BAD9CD6 - 5F5E-4E56 - 990D - 7759FBF1C97F)]";
            return secure;
        }
    }
}
