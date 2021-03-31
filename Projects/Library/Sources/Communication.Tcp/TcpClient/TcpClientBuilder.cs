using System;
using System.Net;
using Mabna.Communication.Tcp.Framework;
using Microsoft.Extensions.Logging;

namespace Mabna.Communication.Tcp.TcpClient
{
    public class TcpClientBuilder : ITcpClientBuilder
    {
        private readonly ILogger<TcpClient> _tcpClientLogger;
        private readonly IPacketParser _packetParser;
        private readonly ICommandOptionsBuilder _commandOptionsBuilder;

        private byte[] _header;

        private byte[] _tail;

        private IPAddress _localIpAddress;

        private IPAddress _ipAddress;

        private int _port;

        public TcpClientBuilder(ILogger<TcpClient> tcpLogger, IPacketParser packetParser, ICommandOptionsBuilder commandOptionsBuilder)
        {
            _packetParser = packetParser;
            _commandOptionsBuilder = commandOptionsBuilder;
            _tcpClientLogger = tcpLogger;
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

        public ITcpClientBuilder LocalIPAddress(IPAddress ipAddress)
        {
            if (_localIpAddress != null)
                throw new Exception("Local IP address is specified more than once.");

            _localIpAddress = ipAddress;

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
            var socketConfig = new SocketConfig(_localIpAddress, _ipAddress, _port);

            return new TcpClient(_tcpClientLogger, socketConfig, packetConfig, _packetParser, _commandOptionsBuilder);
        }

        public ITcpClientBuilder Create()
        {
            return new TcpClientBuilder(_tcpClientLogger, _packetParser, _commandOptionsBuilder);
        }
    }
}
