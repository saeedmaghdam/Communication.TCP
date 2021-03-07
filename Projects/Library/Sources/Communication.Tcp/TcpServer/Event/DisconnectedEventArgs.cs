using System;

namespace Mabna.Communication.Tcp.TcpServer.Event
{
    public class DisconnectedEventArgs : EventArgs
    {
        public System.Net.Sockets.Socket Socket
        {
            get;
            set;
        }
    }
}
