using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;

namespace AIT.PE02.Server.Core.Helpers
{
    public class IPv4Helper
    {
        public static List<string> GetActiveIP4s()
        {
            List<string> activeIps = new List<string>();
            activeIps.Add("127.0.0.1");
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
        public static List<int> AllAvailblePorts(List<int> ports)
        {
            IPGlobalProperties ipProperties = IPGlobalProperties.GetIPGlobalProperties();
            IPEndPoint[] ipEndPoints = ipProperties.GetActiveTcpListeners();
            List<int> ipEndPointsPorts = GetAllPortsFromIpEndpoints(ipEndPoints);


            var usedPorts = ports.Intersect(ipEndPointsPorts).ToList();
            ports.RemoveAll(x => usedPorts.Contains(x));
            return ports;
        }

        private static List<int> GetAllPortsFromIpEndpoints(IPEndPoint[] ipEndPoints)
        {
            List<int> ports = new List<int>();
            foreach (var endPoint in ipEndPoints)
            {
                ports.Add(endPoint.Port);
            }
            return ports;
        }
    }
}
