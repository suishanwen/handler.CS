using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;

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
    }
}
