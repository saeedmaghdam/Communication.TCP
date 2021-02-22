using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using Mabna.Communication.Tcp.Framework;
using Mabna.Communication.Tcp.TcpServer.Event;

namespace Mabna.Communication.Tcp.TcpServer
{
    public class TcpServer : ITcpServer
    {
        private readonly SocketConfig _socketConfig;
        private readonly PacketConfig _packetConfig;
        private readonly IPacketProcessor _packetProcessor;
        private readonly IPacketParser _packetParser;
        private System.Net.Sockets.Socket _listenerSocket;
        private bool _isListening = true;

        private ManualResetEvent _allDone = new ManualResetEvent(false);

        public event EventHandler<DataReceivedEventArgs> DataReceived;
        public event EventHandler<PacketReceivedEventArgs> PacketReceived;

        private void OnDataReceived(DataReceivedEventArgs e)
        {
            EventHandler<DataReceivedEventArgs> handler = DataReceived;
            handler?.Invoke(this, e);
        }

        private void OnPacketReceived(PacketReceivedEventArgs e)
        {
            EventHandler<PacketReceivedEventArgs> handler = PacketReceived;
            handler?.Invoke(this, e);
        }

        public TcpServer(SocketConfig socketConfig, PacketConfig packetConfig, IPacketProcessor packetProcessor, IPacketParser packetParser)
        {
            _packetConfig = packetConfig;
            _packetProcessor = packetProcessor;
            _packetParser = packetParser;
            _socketConfig = socketConfig;
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            await Task.Run(() =>
            {
                var localEndPoint = new IPEndPoint(_socketConfig.IPAddress, _socketConfig.Port);

                _listenerSocket = new System.Net.Sockets.Socket(_socketConfig.IPAddress.AddressFamily, SocketType.Stream, ProtocolType.Tcp);

                try
                {
                    _listenerSocket.Bind(localEndPoint);
                    _listenerSocket.Listen(100);

                    while (_isListening)
                    {
                        _allDone.Reset();

                        _listenerSocket.BeginAccept(
                            new AsyncCallback(AcceptCallback),
                            _listenerSocket);

                        _allDone.WaitOne();
                    }
                }
                catch (Exception ex)
                {
                    // ignored
                }

                try
                {
                    _listenerSocket.Close();
                }
                finally
                {
                    _listenerSocket = null;
                }

                return Task.CompletedTask;
            });
        }

        private void AcceptCallback(IAsyncResult ar)
        {
            _allDone.Set();

            if (!_isListening)
                return;

            var listener = (System.Net.Sockets.Socket)ar.AsyncState;
            var handler = listener?.EndAccept(ar);

            var endPoint = handler.RemoteEndPoint as IPEndPoint;

            var state = new StateObject(handler);
            try
            {
                handler?.BeginReceive(state.Buffer, 0, state.BufferSize, 0, new AsyncCallback(ReceiveCallback), state);
            }
            catch
            {
                return;
            }
        }

        private void ReceiveCallback(IAsyncResult ar)
        {
            var state = (StateObject)ar.AsyncState;
            var handler = state?.Socket;

            if (handler == null)
                return;

            int bytesRead = 0;

            try
            {
                bytesRead = handler.EndReceive(ar);
            }
            catch
            {
                return;
            }

            if (bytesRead <= 0)
                return;

            OnDataReceived(new DataReceivedEventArgs
            {
                Socket = handler,
                BytesReceived = bytesRead,
                Bytes = state.Buffer
            });

            state.Cache.AddRange(state.Buffer);
            _packetProcessor.AddDataAsync(handler, state.Buffer, OnPacketReceived, CancellationToken.None);
            
            handler.BeginReceive(state.Buffer, 0, state.BufferSize, 0, new AsyncCallback(ReceiveCallback), state);
        }

        public void Shutdown()
        {
            _isListening = false;
            _allDone.Set();
        }

        public async ValueTask<PacketModel> GetCommandAsync(CancellationToken cancellationToken)
        {
            return await _packetProcessor.GetCommandAsync(cancellationToken);
        }
    }
}
