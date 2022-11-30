using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;

namespace AIT.PE02.Client.Core.Helpers
{
    public class IPv4Helper
    {
        public static List<string> GetActiveIP4s()
        {
            // hier wordt een LIST gemaakt met alle IP nummers van 
            // je eigen actieve NICs
            // manueel wordt het loopback adres toegevoegd
            List<string> activeIps = new List<string>{"127.0.0.1"};
            var host = Dns.GetHostEntry(Dns.GetHostName());
            foreach (var ip in host.AddressList)
            {
                if (ip.AddressFamily == AddressFamily.InterNetwork)
                {
                    activeIps.Add(ip.ToString());
                }
            }
            return activeIps;
        }

        //public static bool PortInUse(int port)
        //{
        //    bool inUse = false;
        //    IPGlobalProperties ipProperties = IPGlobalProperties.GetIPGlobalProperties();
        //    IPEndPoint[] ipEndPoints = ipProperties.GetActiveTcpListeners();
        //    foreach (IPEndPoint endPoint in ipEndPoints)
        //    {
        //        if (endPoint.Port == port)
        //        {
        //            inUse = true;
        //            break;
        //        }
        //    }
        //    return inUse;
        //}
    }
}
