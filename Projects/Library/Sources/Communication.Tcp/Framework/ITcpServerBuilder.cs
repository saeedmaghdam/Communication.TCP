using System.Net;

namespace Mabna.Communication.Tcp.Framework
{
    public interface ITcpServerBuilder
    {
        ITcpServerBuilder Header(byte[] bytes);

        ITcpServerBuilder Tail(byte[] bytes);

        ITcpServerBuilder IPAddress(IPAddress ipAddress);

        ITcpServerBuilder Port(int port);

        ITcpServer Build();

        ITcpServerBuilder Create();
    }
}
