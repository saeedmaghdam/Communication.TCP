using System;
using System.Threading;
using System.Threading.Tasks;
using Mabna.Communication.Tcp.TcpServer.Event;

namespace Mabna.Communication.Tcp.Framework
{
    public interface IPacketProcessor
    {
        void Initialize(PacketConfig packetConfig);

        ValueTask StartAsync(CancellationToken cancellationToken);

        ValueTask AddDataAsync(System.Net.Sockets.Socket socket, byte[] bytes, Action<PacketReceivedEventArgs> callbackAction, CancellationToken cancellationToken);
    }
}
