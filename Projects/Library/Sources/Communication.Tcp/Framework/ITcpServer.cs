using System;
using System.Threading;
using System.Threading.Tasks;
using Mabna.Communication.Tcp.TcpServer.Event;

namespace Mabna.Communication.Tcp.Framework
{
    public interface ITcpServer
    {
        event EventHandler<DataReceivedEventArgs> DataReceived;
        event EventHandler<PacketReceivedEventArgs> PacketReceived;
        event EventHandler<ConnectedEventArgs> Connected;
        event EventHandler<DisconnectedEventArgs> Disconnected;

        Task StartAsync(CancellationToken cancellationToken);

        void Shutdown();

        PacketModel CreateCommand(byte command, byte[] data);
    }
}
