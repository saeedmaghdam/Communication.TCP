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

        Task StartAsync(CancellationToken cancellationToken);

        void Shutdown();

        ValueTask<PacketModel> GetCommandAsync(CancellationToken cancellationToken);
    }
}
