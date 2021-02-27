using System;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using Mabna.Communication.Tcp.Common;
using Mabna.Communication.Tcp.Framework;
using Mabna.Communication.Tcp.TcpClient.Event;

namespace Mabna.Communication.Tcp.TcpClient
{
    public class TcpClient : ITcpClient
    {
        private readonly SocketConfig _socketConfig;
        private readonly PacketConfig _packetConfig;
        private readonly Socket _socket;
        private readonly IPacketParser _packetParser;

        private readonly AckCommand _ack;

        public event EventHandler<PacketSentEventArgs> PacketSent;
        public event EventHandler<PacketFailedToSendEventArg> PacketFailedToSend;
        public event EventHandler<DataSentEventArg> DataSent;

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

        public TcpClient(SocketConfig socketConfig, PacketConfig packetConfig, IPacketParser packetParser)
        {
            _ack = new AckCommand(packetConfig);

            _socketConfig = socketConfig;
            _packetConfig = packetConfig;
            _packetParser = packetParser;
            _socket = new Socket(_socketConfig.IPAddress.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
            _socket.ReceiveTimeout = (int)TimeSpan.FromSeconds(10).TotalMilliseconds;
            _socket.Bind(new IPEndPoint(_socketConfig.IPAddress, 0));
        }

        public async Task<ClientSendAsyncResult> SendAsync(PacketModel packet, CancellationToken cancellationToken)
        {
            var state = new ClientStateObject(_socket);

            var tryRemaining = 10;

            // Try to connect to socket
            if (!_socket.Connected)
            {
                do
                {
                    try
                    {
                        await _socket.ConnectAsync(_socketConfig.IPAddress, _socketConfig.Port);
                    }
                    catch
                    {
                        await Task.Delay(10, cancellationToken);
                    }
                }
                while (tryRemaining-- == 0);

                if (!_socket.Connected)
                    return RaisePacketFailedToSendEvent(state, packet);
            }

            state.Packet = packet;

            tryRemaining = 10;
            do
            {
                try
                {
                    var sendArgs = new SocketAsyncEventArgs();
                    var bytes = packet.GetBytes().ToArray();
                    sendArgs.SetBuffer(bytes, 0, bytes.Length);
                    await _socket.SendAsync(new ClientSocketAwaitable(sendArgs));

                    OnDataSent(new DataSentEventArg()
                    {
                        Bytes = bytes,
                        BytesSent = bytes.Length,
                        Socket = _socket
                    });

                    var receiveArgs = new SocketAsyncEventArgs();
                    receiveArgs.SetBuffer(state.ReceiveBuffer);
                    await _socket.ReceiveAsync(new ClientSocketAwaitable(receiveArgs));

                    if (receiveArgs.BytesTransferred == 0)
                        return RaisePacketFailedToSendEvent(state, packet);

                    if (_packetParser.TryParse(_packetConfig, state.ReceiveBuffer, _ack.GetBytes().ToArray().Length, out var model))
                    {
                        if (_ack.GetBytes().SequenceEqual(model.GetBytes()))
                        {
                            state.SendAsyncResult = new ClientSendAsyncResult(true);

                            OnPacketSent(new PacketSentEventArgs()
                            {
                                Socket = state.Socket,
                                Packet = state.Packet
                            });

                            return new ClientSendAsyncResult(true);
                        }
                    }

                    return RaisePacketFailedToSendEvent(state, packet);
                }
                catch
                {
                    await Task.Delay(10, cancellationToken);
                }
            }
            while (tryRemaining-- == 0);

            OnPacketFailedToSend(new PacketFailedToSendEventArg()
            {
                Packet = state.Packet,
                Socket = state.Socket
            });

            return RaisePacketFailedToSendEvent(state, packet);
        }

        public async Task<ClientSendAsyncResult> SendCommandAsync(byte command, byte commandOptions, byte[] data, CancellationToken cancellationToken)
        {
            var dataSize = BitConverter.GetBytes(data.Length);
            var commandArray = new byte[1] { command };
            var commandOptionsArray = new byte[1] { commandOptions };
            var crc = Util.CalculateCRC(dataSize, commandArray, commandOptionsArray, data);
            var packetModel = new PacketModel(_packetConfig.Header, dataSize, commandArray, commandOptionsArray, data, crc, _packetConfig.Tail);

            return await SendAsync(packetModel, cancellationToken);
        }

        public async Task<ClientSendAsyncResult> SendCommandAsync(byte command, byte[] data, CancellationToken cancellationToken)
        {
            return await SendCommandAsync(command, 0x00, data, cancellationToken);
        }

        public void Close()
        {
            try
            {
                _socket.Shutdown(SocketShutdown.Both);
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
    }
}
