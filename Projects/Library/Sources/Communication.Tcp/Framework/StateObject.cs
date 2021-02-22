using System.Collections.Generic;
using System.Threading;

namespace Mabna.Communication.Tcp.Framework
{
    public class StateObject
    {
        public int BufferSize => 1024;
        private byte[] _buffer;
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

        public bool IsFinishedReceiving
        {
            get;
            set;
        } = false;

        public StateObject(System.Net.Sockets.Socket socket)
        {
            Socket = socket;
            Buffer = new byte[BufferSize];
        }
    }
}
