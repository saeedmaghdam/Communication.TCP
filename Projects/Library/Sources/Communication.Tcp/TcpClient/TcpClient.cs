using System;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using Mabna.Communication.Tcp.Common;
using Mabna.Communication.Tcp.Framework;
using Mabna.Communication.Tcp.TcpClient.Event;
using Microsoft.Extensions.Logging;

namespace Mabna.Communication.Tcp.TcpClient
{
    public class TcpClient : ITcpClient
    {
        private readonly ILogger<TcpClient> _logger;
        private readonly SocketConfig _socketConfig;
        private readonly PacketConfig _packetConfig;
        private Socket _socket;
        private readonly IPacketParser _packetParser;
        private readonly ICommandOptionsBuilder _commandOptionsBuilder;

        private readonly AckCommand _ack;

        public event EventHandler<PacketSentEventArgs> PacketSent;
        public event EventHandler<PacketFailedToSendEventArg> PacketFailedToSend;
        public event EventHandler<DataSentEventArg> DataSent;

        private const int _totalAttempsCount = 10;

        private void OnPacketSent(PacketSentEventArgs e)
        {
            EventHandler<PacketSentEventArgs> handler = PacketSent;
            handler?.Invoke(this, e);
        }

        private void OnPacketFailedToSend(PacketFailedToSendEventArg e)
        {
            EventHandler<PacketFailedToSendEventArg> handler = PacketFailedToSend;
            handler?.Invoke(this, e);
        }

        private void OnDataSent(DataSentEventArg e)
        {
            EventHandler<DataSentEventArg> handler = DataSent;
            handler?.Invoke(this, e);
        }

        public TcpClient(ILogger<TcpClient> logger, SocketConfig socketConfig, PacketConfig packetConfig, IPacketParser packetParser, ICommandOptionsBuilder commandOptionsBuilder)
        {
            _ack = new AckCommand(packetConfig);

            _logger = logger;

            _socketConfig = socketConfig;
            _packetConfig = packetConfig;
            _packetParser = packetParser;
            _commandOptionsBuilder = commandOptionsBuilder;

            InitializeSocket();
        }

        public async Task<ClientSendAsyncResult> SendAsync(PacketModel packet, CancellationToken cancellationToken)
        {
            var activity = ActivityHelper.Start();
            _logger.LogTrace("Sending command {Command} with command options {CommandOptions} and data {Data}", packet.Command.DisplayByteArrayAsHex(), packet.CommandOptions.DisplayByteArrayAsHex(), packet.Data.DisplayByteArrayAsHex());

            var state = new ClientStateObject(_socket);

            var tryRemaining = _totalAttempsCount;

            // Try to connect to socket
            if (!_socket.Connected)
            {
                _logger.LogTrace("Getting to connect to the socket ...");
                do
                {
                    try
                    {
                        await _socket.ConnectAsync(_socketConfig.IPAddress, _socketConfig.Port);
                    }
                    catch (Exception ex)
                    {
                        if ((ex as SocketException)?.ErrorCode == 10056)
                        {
                            _logger.LogInformation("The Socket is already connected. we initialize the current socket and try again!");

                            _socket.Disconnect(true);
                            _socket.Close();

                            InitializeSocket();
                        }

                        await Task.Delay(10, cancellationToken);
                    }
                }
                while (!_socket.Connected && tryRemaining-- != 0);

                if (!_socket.Connected)
                {
                    _logger.LogError("Failed to connect to the socket.");
                    activity.Stop();
                    return RaisePacketFailedToSendEvent(state, packet);
                }
            }

            state.Packet = packet;

            _logger.LogTrace("Trying to send the packet ...");
            tryRemaining = _totalAttempsCount;
            do
            {
                try
                {
                    var sendArgs = new SocketAsyncEventArgs();
                    var bytes = packet.GetBytes().ToArray();
                    sendArgs.SetBuffer(bytes, 0, bytes.Length);

                    _logger.LogTrace("Socket send arguments has initialized, sending the packet over the network ...");
                    await _socket.SendAsync(new ClientSocketAwaitable(sendArgs));
                    _logger.LogTrace("Sent totally {TotalBytes} bytes over the network.", bytes.Length);
                    await Task.Delay(10, cancellationToken);

                    OnDataSent(new DataSentEventArg()
                    {
                        Bytes = bytes,
                        BytesSent = bytes.Length,
                        Socket = _socket
                    });

                    if (CommandOptions.TryParse(packet.CommandOptions.Single(), out var commandOptions))
                    {
                        if (!commandOptions.AckRequired && !commandOptions.ResponseRequired)
                        {
                            _logger.LogTrace("Packet sent successfully, no response needed.");
                            activity.Stop();
                            return new ClientSendAsyncResult(true);
                        }

                        var receiveArgs = new SocketAsyncEventArgs();
                        receiveArgs.SetBuffer(state.ReceiveBuffer);

                        _logger.LogTrace("Socket receive arguments initialized, receiving data from the network ...");
                        await _socket.ReceiveAsync(new ClientSocketAwaitable(receiveArgs));
                        _logger.LogTrace("Received totally {TotalBytes} bytes from the network.", receiveArgs.BytesTransferred);

                        var isPackedDetected = _packetParser.TryParse(_packetConfig, state.ReceiveBuffer, receiveArgs.BytesTransferred, out var model);

                        if (!isPackedDetected)
                        {
                            _logger.LogTrace("Received data is not parsed as our packet format, going to receive more data from network.");
                            state.ReceiveCache.AddRange(state.ReceiveBuffer.Take(receiveArgs.BytesTransferred));

                            int tryAttempts = 10;
                            while (true)
                            {
                                if (_socket.Available > 0)
                                {
                                    _logger.LogTrace("Receiving data from the networtk ...");
                                    await _socket.ReceiveAsync(new ClientSocketAwaitable(receiveArgs));
                                    _logger.LogTrace("Received totally {TotalBytes} bytes from the network.", receiveArgs.BytesTransferred);
                                    state.ReceiveCache.AddRange(state.ReceiveBuffer.Take(receiveArgs.BytesTransferred));
                                }
                                else
                                {
                                    _logger.LogTrace("There's no data available in NIC buffer.");
                                    await Task.Delay(500, cancellationToken);
                                }
                            }
                        }

                        if (!isPackedDetected && _packetParser.TryParse(_packetConfig, state.ReceiveCache.ToArray(), state.ReceiveCache.Count, out model))
                        {
                            _logger.LogError("Received packet is not in our packet format.");
                            activity.Stop();
                            return RaisePacketFailedToSendEvent(state, packet);
                        }

                        if ((commandOptions.ResponseRequired) || (commandOptions.AckRequired && _ack.GetBytes().SequenceEqual(model.GetBytes()))) // If client has requested response or ack
                        {
                            state.SendAsyncResult = new ClientSendAsyncResult(true, model.Data.ToArray());

                            OnPacketSent(new PacketSentEventArgs()
                            {
                                Socket = state.Socket,
                                Packet = state.Packet
                            });

                            _logger.LogTrace("Packet sent over network and received response successfully.");
                            activity.Stop();
                            return new ClientSendAsyncResult(true, model.Data.ToArray());
                        }
                    }

                    _logger.LogError("Failed to interpret the command options.");
                    activity.Stop();
                    return RaisePacketFailedToSendEvent(state, packet);
                }
                catch
                {
                    await Task.Delay(10, cancellationToken);
                }
            }
            while (tryRemaining-- != 0);

            OnPacketFailedToSend(new PacketFailedToSendEventArg()
            {
                Packet = state.Packet,
                Socket = state.Socket
            });

            _logger.LogError("Failed to send packet over network after {TotalAttempts} retries.", _totalAttempsCount);
            activity.Stop();
            return RaisePacketFailedToSendEvent(state, packet);
        }

        public async Task<ClientSendAsyncResult> SendCommandAsync(byte command, byte commandOptions, byte[] data, CancellationToken cancellationToken)
        {
            var dataSize = BitConverter.GetBytes(data.Length);
            var commandArray = new byte[] { command };
            var commandOptionsArray = new byte[] { commandOptions };
            var crc = Util.CalculateCRC(dataSize, commandArray, commandOptionsArray, data);
            var packetModel = new PacketModel(_packetConfig.Header, dataSize, commandArray, commandOptionsArray, data, crc, _packetConfig.Tail);

            return await SendAsync(packetModel, cancellationToken);
        }

        public async Task<ClientSendAsyncResult> SendCommandAsync(byte command, byte[] data, CancellationToken cancellationToken)
        {
            var commandOptions = _commandOptionsBuilder.AckRequired(false).Build();

            return await SendCommandAsync(command, commandOptions, data, cancellationToken);
        }

        public void Close()
        {
            try
            {
                _socket.Disconnect(true);
                _socket.Close();
            }
            catch
            {
                // ignored
            }
        }

        private ClientSendAsyncResult RaisePacketFailedToSendEvent(ClientStateObject state, PacketModel packet)
        {
            OnPacketFailedToSend(new PacketFailedToSendEventArg()
            {
                Socket = state.Socket,
                Packet = packet
            });

            return state.SendAsyncResult;
        }

        private void InitializeSocket()
        {
            _socket = new Socket(_socketConfig.IPAddress.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
            _socket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
            _socket.ReceiveTimeout = (int)TimeSpan.FromSeconds(10).TotalMilliseconds;
            _socket.Bind(new IPEndPoint(_socketConfig.LocalIPAddress, 0));
        }
    }
}
