using System;
using System.Net;
using System.Net.Sockets;

namespace UpRestEye3.Services.BusinessLogic
{

    public static class NetworkHelper
    {
        public static string GetLocalIpAddress()
        {
            var host = Dns.GetHostEntry(Dns.GetHostName());
            foreach (var ip in host.AddressList)
            {
                if (ip.AddressFamily == AddressFamily.InterNetwork)
                {
                    return ip.ToString();
                }
            }
            throw new Exception("Local IP Address Not Found!");
        }

        public class AppConfig
        {
            public string IpAddress { get; set; }
            public int HttpPort { get; set; } = 5127;
            public int HttpsPort { get; set; } = 7124;
        }

    }

}
