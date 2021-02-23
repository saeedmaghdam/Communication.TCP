using System.Net.Sockets;

namespace Mabna.Communication.Tcp.TcpClient
{
    public static class ClientExtensions
    {
        public static ClientSocketAwaitable ReceiveAsync(this Socket socket, ClientSocketAwaitable awaitable)
        {
            awaitable.Reset();
            if (!socket.ReceiveAsync(awaitable.EventArgs))
                awaitable.IsCompleted = true;
            return awaitable;
        }

        public static ClientSocketAwaitable SendAsync(this Socket socket, ClientSocketAwaitable awaitable)
        {
            awaitable.Reset();
            if (!socket.SendAsync(awaitable.EventArgs))
                awaitable.IsCompleted = true;
            return awaitable;
        }
    }
}
