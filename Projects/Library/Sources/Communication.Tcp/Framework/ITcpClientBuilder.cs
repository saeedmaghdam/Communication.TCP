using System.Net;

namespace Mabna.Communication.Tcp.Framework
{
    public interface ITcpClientBuilder
    {
        ITcpClientBuilder Header(byte[] bytes);

        ITcpClientBuilder Tail(byte[] bytes);

        ITcpClientBuilder IPAddress(IPAddress ipAddress);

        ITcpClientBuilder Port(int port);

        ITcpClient Build();

        ITcpClientBuilder Create();
    }
}
