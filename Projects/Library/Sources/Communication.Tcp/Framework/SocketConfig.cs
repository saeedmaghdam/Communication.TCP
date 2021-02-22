using System.Net;

namespace Mabna.Communication.Tcp.Framework
{
    public class SocketConfig
    {
        public IPAddress IPAddress
        {
            get;
        }

        public int Port
        {
            get;
        }

        public SocketConfig(IPAddress ipAddress, int port)
        {
            IPAddress = ipAddress;
            Port = port;

            IPAddress ??= Dns.GetHostEntry(Dns.GetHostName()).AddressList[0];
        }
    }
}
