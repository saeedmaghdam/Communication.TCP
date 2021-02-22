using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Net;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using Mabna.Communication.Tcp.Framework;
using Mabna.Communication.Tcp.TcpServer.Event;

namespace Mabna.Communication.Tcp.PacketProcessor
{
    public class PacketProcessor : IPacketProcessor
    {
        private enum State : short
        {
            Header = 0,
            DataSize = 1,
            Command = 2,
            Data = 3,
            Crc = 4,
            Tail = 5
        }

        private readonly IPacketParser _packetParser;
        private PacketConfig _packetConfig;
        private readonly Channel<Tuple<System.Net.Sockets.Socket, byte[]>> _endPointBytesChannel;
        private readonly Channel<PacketModel> _commandChannel;
        private Dictionary<EndPoint, List<byte>> _buffer = new Dictionary<EndPoint, List<byte>>();
        private State _state;
        private int _stateIndex;
        private List<byte> _dataSize;
        private delegate void _onPacketReceivedDelegate(PacketReceivedEventArgs args);

        private ConcurrentDictionary<System.Net.Sockets.Socket, Action<PacketReceivedEventArgs>> _callbackActionsDictionary;

        public PacketProcessor(IPacketParser packetParser)
        {
            _packetParser = packetParser;

            _callbackActionsDictionary = new ConcurrentDictionary<System.Net.Sockets.Socket, Action<PacketReceivedEventArgs>>();

            _endPointBytesChannel = Channel.CreateBounded<Tuple<System.Net.Sockets.Socket, byte[]>>(new BoundedChannelOptions(100)
            {
                FullMode = BoundedChannelFullMode.Wait
            });

            _commandChannel = Channel.CreateBounded<PacketModel>(new BoundedChannelOptions(1024)
            {
                FullMode = BoundedChannelFullMode.Wait
            });
        }

        public void Initialize(PacketConfig packetConfig)
        {
            _packetConfig = packetConfig;
        }

        public async ValueTask StartAsync(CancellationToken cancellationToken)
        {
            if (_packetConfig == null)
                throw new Exception("Packet processor is not initialized yet.");

            _state = State.Header;
            _stateIndex = 1;
            _dataSize = new List<byte>();

            do
            {
                var endPointBytes = await _endPointBytesChannel.Reader.ReadAsync(cancellationToken);
                await ProcessAsync(endPointBytes.Item1, endPointBytes.Item2, cancellationToken);
            }
            while (true);
        }

        public async ValueTask AddDataAsync(System.Net.Sockets.Socket socket, byte[] bytes, Action<PacketReceivedEventArgs> callbackAction, CancellationToken cancellationToken)
        {
            if (!_callbackActionsDictionary.TryGetValue(socket, out var action))
                _callbackActionsDictionary.TryAdd(socket, callbackAction);

            await _endPointBytesChannel.Writer.WriteAsync(new Tuple<System.Net.Sockets.Socket, byte[]>(socket, bytes), cancellationToken);
        }

        public async ValueTask<PacketModel> GetCommandAsync(CancellationToken cancellationToken)
        {
            return await _commandChannel.Reader.ReadAsync(cancellationToken);
        }

        private async ValueTask ProcessAsync(System.Net.Sockets.Socket socket, byte[] bytes, CancellationToken cancellationToken)
        {
            var endPoint = socket.RemoteEndPoint;

            if (!_buffer.ContainsKey(endPoint))
                _buffer.Add(endPoint, new List<byte>());

            var isTailSeen = false;
            foreach (var b in bytes)
            {
                if (isTailSeen)
                    break;

                _buffer[endPoint].Add(b);

                switch (_state)
                {
                    case State.Header:
                        if (_stateIndex < _packetConfig.Header.Length)
                        {
                            _stateIndex++;
                        }
                        else
                        {
                            _stateIndex = 1;
                            _state++;
                        }

                        break;
                    case State.DataSize:
                        if (_stateIndex < _packetConfig.DataMaxSize)
                        {
                            _stateIndex++;
                        }
                        else
                        {
                            _stateIndex = 1;
                            _state++;
                        }

                        _dataSize.Add(b);

                        break;
                    case State.Command:
                        _stateIndex = 1;
                        _state++;

                        break;
                    case State.Data:
                        if (_stateIndex < BitConverter.ToInt32(_dataSize.ToArray()))
                        {
                            _stateIndex++;
                        }
                        else
                        {
                            _stateIndex = 1;
                            _state++;
                        }

                        break;
                    case State.Crc:
                        _stateIndex = 1;
                        _state++;

                        break;
                    case State.Tail:
                        if (_stateIndex < _packetConfig.Tail.Length)
                        {
                            _stateIndex++;
                        }
                        else
                        {
                            _stateIndex = 1;
                            _state = 0;

                            if (_packetParser.TryParse(_packetConfig, _buffer[endPoint].ToArray(), out var packetModel))
                            {
                                await _commandChannel.Writer.WriteAsync(packetModel, cancellationToken);
                                _buffer.Clear();

                                if (_callbackActionsDictionary.TryGetValue(socket, out var action))
                                {
                                    action.Invoke(new PacketReceivedEventArgs()
                                    {
                                        Packet = packetModel,
                                        Socket = socket
                                    });
                                }
                            }

                            isTailSeen = true;
                        }

                        break;
                }
            }
        }
    }
}
