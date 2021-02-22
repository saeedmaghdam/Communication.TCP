using System;
using Mabna.Communication.Tcp.Framework;

namespace Mabna.Communication.Tcp.TcpClient.Event
{
    public class PacketSentEventArgs : EventArgs
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
