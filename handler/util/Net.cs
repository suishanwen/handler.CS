using System;
using System.IO;
using System.Net.NetworkInformation;
using System.Runtime.InteropServices;

namespace handler.util
{
    class Net
    {
        private const int INTERNET_CONNECTION_MODEM = 1;
        private const int INTERNET_CONNECTION_LAN = 2;
        [DllImport("winInet.dll")]
        private static extern bool InternetGetConnectedState(
        ref int dwFlag,
        int dwReserved);
        public static bool isOnline()
        {
            System.Int32 dwFlag = new int();
            if (!InternetGetConnectedState(ref dwFlag, 0))
            {
                Console.WriteLine("未连网");
                return false;
            }
            else if ((dwFlag & INTERNET_CONNECTION_MODEM) != 0)
            {
                Console.WriteLine("采用调制解调器上网");
                return true;
            }
            else
            {
                Console.WriteLine("采用网卡上网");
                return false;
            }
        }


        public static bool isRealOnline()
        {
            System.Int32 dwFlag = new int();
            if (!InternetGetConnectedState(ref dwFlag, 0))
            {
                Console.WriteLine("未连网");
                return false;
            }
            else if ((dwFlag & INTERNET_CONNECTION_MODEM) != 0)
            {
                Console.WriteLine("采用调制解调器上网");
                return true;
            }
            else
            {
                Console.WriteLine("采用网卡上网");
                return true;
            }
        }

        public static long GetNetStatic(String adslName)
        {
            NetworkInterface[] nics = NetworkInterface.GetAllNetworkInterfaces();
            if (nics == null || nics.Length < 1)
            {
                Console.WriteLine("  No network interfaces found.");
            }
            foreach (NetworkInterface adapter in nics)
            {
                if (adapter.Name == adslName)
                {
                    try
                    {
                        IPv4InterfaceStatistics ipv4Statistics = adapter.GetIPv4Statistics();
                        long send = ipv4Statistics.BytesSent / 1024;
                        long recv = ipv4Statistics.BytesReceived / 1024;
                        return send + recv;
                    }
                    catch (Exception)
                    {
                        return 0;
                    }
                }
            }
            return 0;
        }
    }
}
