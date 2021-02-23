using System;
using System.Net;
using System.Threading;
using Mabna.Communication.Tcp.Framework;

namespace Mabna.Communication.Tcp.TcpServer
{
    public class TcpServerBuilder : ITcpServerBuilder
    {
        private readonly IPacketParser _packetParser;
        private readonly IPacketProcessor _packetProcessor;

        private byte[] _header;

        private byte[] _tail;

        private IPAddress _ipAddress;

        private int _port;

        public TcpServerBuilder(IPacketParser packetParser, IPacketProcessor packetProcessor)
        {
            _packetParser = packetParser;
            _packetProcessor = packetProcessor;
        }

        public ITcpServerBuilder Header(byte[] bytes)
        {
            if (_header != null)
                throw new Exception("Header is specified more than once.");

            _header = bytes;

            return this;
        }

        public ITcpServerBuilder Tail(byte[] bytes)
        {
            if (_tail != null)
                throw new Exception("Tail is specified more than once.");

            _tail = bytes;

            return this;
        }

        public ITcpServerBuilder IPAddress(IPAddress ipAddress)
        {
            if (_ipAddress != null)
                throw new Exception("IP address is specified more than once.");

            _ipAddress = ipAddress;

            return this;
        }

        public ITcpServerBuilder Port(int port)
        {
            if (_port != 0)
                throw new Exception("Port is specified more than once.");

            _port = port;

            return this;
        }

        public ITcpServer Build()
        {
            var packetConfig = new PacketConfig(_header, _tail);
            var socketConfig = new SocketConfig(_ipAddress, _port);

            _packetProcessor.Initialize(packetConfig);
            _packetProcessor.StartAsync(CancellationToken.None);

            return new TcpServer(socketConfig, _packetProcessor);
        }

        public ITcpServerBuilder Create()
        {
            return new TcpServerBuilder(_packetParser, _packetProcessor);
        }
    }
}
