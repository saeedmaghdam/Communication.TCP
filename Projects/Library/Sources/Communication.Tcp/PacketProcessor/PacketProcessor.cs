using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
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
            CommandOptions = 3,
            Data = 4,
            Crc = 5,
            Tail = 6
        }

        private readonly IPacketParser _packetParser;
        private PacketConfig _packetConfig;
        private readonly Channel<Tuple<System.Net.Sockets.Socket, List<byte>>> _endPointBytesChannel;
        private Dictionary<EndPoint, Tuple<List<byte>, List<byte>>> _buffer = new Dictionary<EndPoint, Tuple<List<byte>, List<byte>>>(); // Buffer - DataSize
        private State _state;
        private int _stateIndex;
        private delegate void _onPacketReceivedDelegate(PacketReceivedEventArgs args);
        private AckCommand _ack;

        private ConcurrentDictionary<System.Net.Sockets.Socket, Action<PacketReceivedEventArgs>> _callbackActionsDictionary;

        public PacketProcessor(IPacketParser packetParser)
        {
            _packetParser = packetParser;

            _callbackActionsDictionary = new ConcurrentDictionary<System.Net.Sockets.Socket, Action<PacketReceivedEventArgs>>();

            _endPointBytesChannel = Channel.CreateBounded<Tuple<System.Net.Sockets.Socket, List<byte>>>(new BoundedChannelOptions(10000)
            {
                FullMode = BoundedChannelFullMode.Wait
            });
        }

        public void Initialize(PacketConfig packetConfig)
        {
            _ack = new AckCommand(packetConfig);
            _packetConfig = packetConfig;
        }

        public async ValueTask StartAsync(CancellationToken cancellationToken)
        {
            if (_packetConfig == null)
                throw new Exception("Packet processor is not initialized yet.");

            _state = State.Header;
            _stateIndex = 1;

            do
            {
                var endPointBytes = await _endPointBytesChannel.Reader.ReadAsync(cancellationToken);

                await ProcessAsync(endPointBytes.Item1, endPointBytes.Item2, cancellationToken);
            }
            while (true);
        }

        public async ValueTask AddDataAsync(System.Net.Sockets.Socket socket, byte[] bytes, int bytesReceived, Action<PacketReceivedEventArgs> callbackAction, CancellationToken cancellationToken)
        {
            if (!_callbackActionsDictionary.TryGetValue(socket, out var action))
                _callbackActionsDictionary.TryAdd(socket, callbackAction);

            await _endPointBytesChannel.Writer.WriteAsync(new Tuple<System.Net.Sockets.Socket, List<byte>>(socket, bytes.Take(bytesReceived).ToList()), cancellationToken);
        }

        private async ValueTask ProcessAsync(System.Net.Sockets.Socket socket, List<byte> bytes, CancellationToken cancellationToken)
        {
            var endPoint = socket.RemoteEndPoint;

            if (!_buffer.ContainsKey(endPoint))
                _buffer.Add(endPoint, new Tuple<List<byte>, List<byte>>(new List<byte>(), new List<byte>()));

            var isTailSeen = false;
            foreach (var b in bytes)
            {
                if (isTailSeen)
                    break;

                _buffer[endPoint].Item1.Add(b);

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

                        _buffer[endPoint].Item2.Add(b);

                        break;
                    case State.Command:
                        _stateIndex = 1;
                        _state++;

                        if (BitConverter.ToInt32(_buffer[endPoint].Item2.ToArray()) == 0) // We're skipping data section if we've not received any data (DATA_SIZE == 0)
                            _state++;

                        break;
                    case State.CommandOptions:
                        _stateIndex = 1;
                        _state++;

                        break;
                    case State.Data:
                        if (_stateIndex < BitConverter.ToInt32(_buffer[endPoint].Item2.ToArray()))
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

                            if (_callbackActionsDictionary.TryGetValue(socket, out var action))
                            {
                                if (_packetParser.TryParse(_packetConfig, _buffer[endPoint].Item1.ToArray(), out var packetModel))
                                {
                                    action.Invoke(new PacketReceivedEventArgs()
                                    {
                                        Packet = packetModel,
                                        Socket = socket
                                    });

                                    // Send ack to client
                                    System.Net.Sockets.SocketAsyncEventArgs arg = new System.Net.Sockets.SocketAsyncEventArgs();
                                    arg.SetBuffer(_ack.GetBytes().ToArray());
                                    socket.SendAsync(arg);
                                }
                                else
                                {
                                    // Send a zero-byte message to unblock the client.
                                    System.Net.Sockets.SocketAsyncEventArgs arg = new System.Net.Sockets.SocketAsyncEventArgs();
                                    arg.SetBuffer(new byte[] { });
                                    socket.SendAsync(arg);
                                }
                            }

                            _buffer[endPoint] = new Tuple<List<byte>, List<byte>>(new List<byte>(), new List<byte>());

                            isTailSeen = true;
                        }

                        break;
                }
            }
        }
    }
}
