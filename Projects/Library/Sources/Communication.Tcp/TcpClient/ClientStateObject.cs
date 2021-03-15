using System.Collections.Generic;
using System.Threading;
using Mabna.Communication.Tcp.Framework;

namespace Mabna.Communication.Tcp.TcpClient
{
    public class ClientStateObject
    {
        public int BufferSize => 1024;
        private byte[] _buffer;
        private List<byte> _receiveCache = new List<byte>();
        private List<byte> _cache = new List<byte>();

        public byte[] Buffer
        {
            get => _buffer;
            set => _buffer = value;
        }

        public List<byte> Cache => _cache;

        public System.Net.Sockets.Socket Socket
        {
            get;
        }

        public CancellationTokenSource CancellationTokenSource
        {
            get;
        } = new CancellationTokenSource();

        public PacketModel Packet
        {
            get;
            set;
        }

        public int BytesSent
        {
            get;
            set;
        }

        public byte[] ReceiveBuffer
        {
            get;
            set;
        }

        public List<byte> ReceiveCache
        {
            get;
            set;
        }

        public ClientSendAsyncResult SendAsyncResult
        {
            get;
            set;
        } = new ClientSendAsyncResult(false);

        public ClientStateObject(System.Net.Sockets.Socket socket)
        {
            Socket = socket;
            Buffer = new byte[BufferSize];
            ReceiveBuffer = new byte[BufferSize];
        }
    }

    public class ClientSendAsyncResult
    {
        public bool IsSent
        {
            get;
        }

        public byte[] Response
        {
            get;
        }

        public ClientSendAsyncResult(bool isSent, byte[] response)
        {
            IsSent = isSent;
            Response = response;
        }

        public ClientSendAsyncResult(bool isSent)
        {
            IsSent = isSent;
            Response = new byte[] { };
        }

        public static ClientSendAsyncResult CreateDefaultInstance()
        {
            return new ClientSendAsyncResult(false, new byte[] { });
        }
    }
}
