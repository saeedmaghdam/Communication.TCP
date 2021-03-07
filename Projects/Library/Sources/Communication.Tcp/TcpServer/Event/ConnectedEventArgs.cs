using System;

namespace Mabna.Communication.Tcp.TcpServer.Event
{
    public class ConnectedEventArgs : EventArgs
    {
        public System.Net.Sockets.Socket Socket
        {
            get;
            set;
        }
    }
}
