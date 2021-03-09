﻿using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using Mabna.Communication.Tcp.Common;
using Mabna.Communication.Tcp.Framework;
using Mabna.Communication.Tcp.TcpServer.Event;

namespace Mabna.Communication.Tcp.TcpServer
{
    public class TcpServer : ITcpServer
    {
        private readonly PacketConfig _packetConfig;
        private readonly SocketConfig _socketConfig;
        private readonly IPacketProcessor _packetProcessor;
        private Socket _listenerSocket;
        private bool _isListening = true;

        private static ManualResetEvent _allDone = new ManualResetEvent(false);

        public event EventHandler<DataReceivedEventArgs> DataReceived;
        public event EventHandler<PacketReceivedEventArgs> PacketReceived;
        public event EventHandler<ConnectedEventArgs> Connected;
        public event EventHandler<DisconnectedEventArgs> Disconnected;

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

        private void OnConnected(ConnectedEventArgs e)
        {
            EventHandler<ConnectedEventArgs> handler = Connected;
            handler?.Invoke(this, e);
        }

        private void OnDisconnected(DisconnectedEventArgs e)
        {
            EventHandler<DisconnectedEventArgs> handler = Disconnected;
            handler?.Invoke(this, e);
        }

        public TcpServer(PacketConfig packetConfig, SocketConfig socketConfig, IPacketProcessor packetProcessor)
        {
            _packetProcessor = packetProcessor;
            _socketConfig = socketConfig;
            _packetConfig = packetConfig;
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            var thread = new Thread(() =>
            {
                var localEndPoint = new IPEndPoint(_socketConfig.IPAddress, _socketConfig.Port);

                _listenerSocket = new Socket(_socketConfig.IPAddress.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
                _listenerSocket.SendTimeout = (int)TimeSpan.FromSeconds(5).TotalMilliseconds;
                _listenerSocket.ReceiveTimeout = (int)TimeSpan.FromSeconds(5).TotalMilliseconds;

                try
                {
                    _listenerSocket.Bind(localEndPoint);
                    _listenerSocket.Listen(100);

                    while (_isListening)
                    {
                        _allDone.Reset();

                        _listenerSocket.BeginAccept(new AsyncCallback(AcceptCallback), _listenerSocket);

                        _allDone.WaitOne();
                    }
                }
                catch
                {
                    // ignored
                }

                try
                {
                    _listenerSocket.Close();
                }
                catch
                {
                    // ignored
                }
                finally
                {
                    _listenerSocket = null;
                }
            });

            thread.Start();

            do
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    try
                    {
                        thread.Abort();
                    }
                    catch
                    {
                        // ignored
                    }

                    break;
                }

                try
                {
                    await Task.Delay(3_600_000, cancellationToken);
                }
                catch
                {
                    // ignored
                }
            }
            while (true);
        }

        private void AcceptCallback(IAsyncResult ar)
        {
            _allDone.Set();
            Socket listener = default(Socket);
            try
            {
                listener = (Socket)ar.AsyncState;
                var handler = listener?.EndAccept(ar);

                OnConnected(new ConnectedEventArgs()
                {
                    Socket = handler
                });

                var state = new StateObject(handler);

                //return;

                handler?.BeginReceive(state.Buffer, 0, state.BufferSize, 0, new AsyncCallback(ReceiveCallback), state);
            }
            catch (SocketException ex)
            {
                if (ex.ErrorCode == 10054)
                {
                    OnDisconnected(new DisconnectedEventArgs()
                    {
                        Socket = listener
                    });
                }
            }
            catch
            {
                // ignored
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
            catch (SocketException ex)
            {
                if (ex.ErrorCode == 10054)
                {
                    OnDisconnected(new DisconnectedEventArgs()
                    {
                        Socket = handler
                    });
                }
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

            _packetProcessor.AddDataAsync(handler, state.Buffer, bytesRead, OnPacketReceived, CancellationToken.None);

            try
            {
                handler.BeginReceive(state.Buffer, 0, state.BufferSize, 0, new AsyncCallback(ReceiveCallback), state);
            }
            catch (SocketException ex)
            {
                if (ex.ErrorCode == 10054)
                {
                    OnDisconnected(new DisconnectedEventArgs()
                    {
                        Socket = handler
                    });
                }
            }
            catch
            {
                // ignored
            }
        }

        public void Shutdown()
        {
            _isListening = false;
            _allDone.Set();
        }

        public PacketModel CreateCommand(byte command, byte commandOptions, byte[] data)
        {
            var dataSize = BitConverter.GetBytes(data.Length);
            var commandArray = new byte[] { command };
            var commandOptionsArray = new byte[] { commandOptions };
            var crc = Util.CalculateCRC(dataSize, commandArray, commandOptionsArray, data);
            var packetModel = new PacketModel(_packetConfig.Header, dataSize, commandArray, commandOptionsArray, data, crc, _packetConfig.Tail);

            return packetModel;
        }

        public PacketModel CreateCommand(byte command, byte[] data)
        {
            return CreateCommand(command, 0x00, data);
        }
    }
}
