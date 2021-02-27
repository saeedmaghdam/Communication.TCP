using System;
using System.Net;
using Mabna.Communication.Tcp.Framework;

namespace Mabna.Communication.Tcp.TcpClient
{
    public class TcpClientBuilder : ITcpClientBuilder
    {
        private readonly IPacketParser _packetParser;
        private readonly ICommandOptionsBuilder _commandOptionsBuilder;

        private byte[] _header;

        private byte[] _tail;

        private IPAddress _ipAddress;

        private int _port;

        public TcpClientBuilder(IPacketParser packetParser, ICommandOptionsBuilder commandOptionsBuilder)
        {
            _packetParser = packetParser;
            _commandOptionsBuilder = commandOptionsBuilder;
        }

        public ITcpClientBuilder Header(byte[] bytes)
        {
            if (_header != null)
                throw new Exception("Header is specified more than once.");

            _header = bytes;

            return this;
        }

        public ITcpClientBuilder Tail(byte[] bytes)
        {
            if (_tail != null)
                throw new Exception("Tail is specified more than once.");

            _tail = bytes;

            return this;
        }

        public ITcpClientBuilder IPAddress(IPAddress ipAddress)
        {
            if (_ipAddress != null)
                throw new Exception("IP address is specified more than once.");

            _ipAddress = ipAddress;

            return this;
        }

        public ITcpClientBuilder Port(int port)
        {
            if (_port != 0)
                throw new Exception("Port is specified more than once.");

            _port = port;

            return this;
        }

        public ITcpClient Build()
        {
            var packetConfig = new PacketConfig(_header, _tail);
            var socketConfig = new SocketConfig(_ipAddress, _port);

            return new TcpClient(socketConfig, packetConfig, _packetParser, _commandOptionsBuilder);
        }

        public ITcpClientBuilder Create()
        {
            return new TcpClientBuilder(_packetParser, _commandOptionsBuilder);
        }
    }
}
