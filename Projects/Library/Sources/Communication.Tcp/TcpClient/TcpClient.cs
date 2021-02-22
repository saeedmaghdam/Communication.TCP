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
        private readonly System.Net.Sockets.Socket _socket;

        public event EventHandler<PacketSentEventArgs> PacketSent;
        public event EventHandler<PacketFailedToSendEventArg> PacketFailedToSend;

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

        public TcpClient(SocketConfig socketConfig, PacketConfig packetConfig, IPacketParser packetProcessor)
        {
            _socketConfig = socketConfig;
            _packetConfig = packetConfig;
            _socket = new System.Net.Sockets.Socket(_socketConfig.IPAddress.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
            _socket.ReceiveTimeout = (int)TimeSpan.FromSeconds(10).TotalMilliseconds;
            _socket.Bind(new IPEndPoint(_socketConfig.IPAddress, 0));
        }

        public async Task<bool> SendAsync(PacketModel packet, CancellationToken cancellationToken)
        {
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
                {
                    RaisePacketFailedToSendEvent(_socket, packet);
                    return false;
                }
            }

            var data = packet.GetBytes().ToArray();

            var state = new StateObject(_socket);
            state.Packet = packet;

            tryRemaining = 10;
            var isSent = false;
            do
            {
                try
                {
                    _socket.BeginSend(data, 0, data.Length, SocketFlags.None, new AsyncCallback(SendCallback), state);
                    isSent = true;
                }
                catch
                {
                    await Task.Delay(10, cancellationToken);
                }
            }
            while (tryRemaining-- == 0);

            if (!isSent)
            {
                RaisePacketFailedToSendEvent(_socket, packet);
                return false;
            }

            return true;
        }
        private void SendCallback(IAsyncResult ar)
        {
            try
            {
                var state = (StateObject)ar.AsyncState;
                var socket = state.Socket;

                socket.EndSend(ar);

                OnPacketSent(new PacketSentEventArgs()
                {
                    Socket = socket,
                    Packet = state.Packet
                });
            }
            catch
            {
                // ignored
            }
        }

        public async Task<bool> SendCommandAsync(byte command, byte[] data, CancellationToken cancellationToken)
        {
            var dataSize = BitConverter.GetBytes(data.Length);
            var commandArray = new byte[1] { command };
            var crc = Util.CalculateCRC(dataSize, commandArray, data);
            var packetModel = new PacketModel(_packetConfig.Header, dataSize, commandArray, data, crc, _packetConfig.Tail);

            return await SendAsync(packetModel, cancellationToken);
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

        private void RaisePacketFailedToSendEvent(Socket socket, PacketModel packet)
        {
            OnPacketFailedToSend(new PacketFailedToSendEventArg()
            {
                Socket = socket,
                Packet = packet
            });
        }
    }
}
