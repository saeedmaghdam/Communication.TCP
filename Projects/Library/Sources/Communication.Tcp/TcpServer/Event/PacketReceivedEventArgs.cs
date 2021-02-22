using System;
using Mabna.Communication.Tcp.Framework;

namespace Mabna.Communication.Tcp.TcpServer.Event
{
    public class PacketReceivedEventArgs : EventArgs
    {
        public System.Net.Sockets.Socket Socket
        {
            get;
            set;
        }

        public PacketModel Packet
        {
            get;
            set;
        }
    }
}
