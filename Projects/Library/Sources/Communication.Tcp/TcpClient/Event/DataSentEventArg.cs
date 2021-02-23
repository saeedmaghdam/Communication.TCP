using System;

namespace Mabna.Communication.Tcp.TcpClient.Event
{
    public class DataSentEventArg : EventArgs
    {
        public System.Net.Sockets.Socket Socket
        {
            get;
            set;
        }

        public int BytesSent
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
