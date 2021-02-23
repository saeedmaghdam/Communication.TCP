using System;
using System.Threading;
using System.Threading.Tasks;
using Mabna.Communication.Tcp.TcpClient;
using Mabna.Communication.Tcp.TcpClient.Event;

namespace Mabna.Communication.Tcp.Framework
{
    public interface ITcpClient
    {
        event EventHandler<PacketSentEventArgs> PacketSent;

        event EventHandler<PacketFailedToSendEventArg> PacketFailedToSend;

        event EventHandler<DataSentEventArg> DataSent;

        Task<ClientSendAsyncResult> SendAsync(PacketModel packet, CancellationToken cancellationToken);

        Task<ClientSendAsyncResult> SendCommandAsync(byte command, byte[] data, CancellationToken cancellationToken);

        void Close();
    }
}
