using System.Net;

namespace Mabna.Communication.Tcp.Framework
{
    public class SocketConfig
    {
        public IPAddress LocalIPAddress
        {
            get;
        }

        public IPAddress IPAddress
        {
            get;
        }

        public int Port
        {
            get;
        }

        public SocketConfig(IPAddress localIpAddress, IPAddress ipAddress, int port)
        {
            LocalIPAddress = localIpAddress;
            IPAddress = ipAddress;
            Port = port;

            IPAddress ??= Dns.GetHostEntry(Dns.GetHostName()).AddressList[0];
            LocalIPAddress ??= Dns.GetHostEntry(Dns.GetHostName()).AddressList[0];
        }

        public SocketConfig(IPAddress ipAddress, int port)
        {
            IPAddress = ipAddress;
            Port = port;

            IPAddress ??= Dns.GetHostEntry(Dns.GetHostName()).AddressList[0];
            LocalIPAddress ??= Dns.GetHostEntry(Dns.GetHostName()).AddressList[0];
        }
    }
}
