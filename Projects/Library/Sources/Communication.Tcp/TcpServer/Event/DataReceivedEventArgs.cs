using System;

namespace Mabna.Communication.Tcp.TcpServer.Event
{
    public class DataReceivedEventArgs : EventArgs
    {
        public System.Net.Sockets.Socket Socket
        {
            get;
            set;
        }

        public int BytesReceived
        {
            get;
            set;
        }

        public byte[] Bytes
        {
            get;
            set;
        }
    }
}
