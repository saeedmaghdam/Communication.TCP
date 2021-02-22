using System;
using System.Threading;
using System.Threading.Tasks;
using Mabna.Communication.Tcp.TcpClient.Event;

namespace Mabna.Communication.Tcp.Framework
{
    public interface ITcpClient
    {
        event EventHandler<PacketSentEventArgs> PacketSent;

        Task<bool> SendAsync(PacketModel packet, CancellationToken cancellationToken);

        Task<bool> SendCommandAsync(byte command, byte[] data, CancellationToken cancellationToken);

        void Close();
    }
}
