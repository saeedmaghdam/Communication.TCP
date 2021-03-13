using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Mabna.Communication.Tcp.Framework;
using Mabna.Communication.Tcp.TcpClient;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Communication.Tcp.Tests
{
    public class Worker : IHostedService
    {
        private const int TOTAL_THREADS = 100;
        private const int TOTAL_PACKETS_PER_THREAD = 1000;

        private readonly ITcpServerBuilder _tcpServerBuilder;
        private readonly ITcpClientBuilder _tcpClientBuilder;
        private readonly ILogger<Worker> _logger;
        private readonly ITcpServer _tcpServer;
        private readonly IPAddress _ipAddress;

        private long _totalPacketsCountedByPacketSentEvent;
        private long _totalPacketsCountedByThread;
        private int _totalPacketsReceivedByListener;
        private long _totalPacketsFailedToSend;

        private Stopwatch _sw;

        private long _totalBytesSent;
        private long _totalBytesReceived;

        private List<Thread> _threads;
        
        private bool _isFinished = false;

        public Worker(ILogger<Worker> logger, ITcpServerBuilder tcpServerBuilder, ITcpClientBuilder tcpClientBuilder)
        {
            _logger = logger;
            _tcpServerBuilder = tcpServerBuilder;
            _tcpClientBuilder = tcpClientBuilder;

            _ipAddress = IPAddress.Parse("127.0.0.1");
            _tcpServer = _tcpServerBuilder.IPAddress(_ipAddress).Port(11000).Build();
            _totalPacketsReceivedByListener = 0;
            var stopWatch = new Stopwatch();

            _tcpServer.PacketReceived += (sender, args) =>
            {
                var packet = args.Packet;

                var commandArray = packet.GetBytes().ToArray();
                if (commandArray.Any())
                    _logger.LogInformation($"{string.Join(" ", BitConverter.ToString(commandArray).Split("-").Select(x => "0x" + x))}\r\nPackets received: {++_totalPacketsReceivedByListener}");
            };

            _tcpServer.DataReceived += (sender, args) =>
            {
                Interlocked.Add(ref _totalBytesReceived, args.BytesReceived);
            };
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            _tcpServer.StartAsync(cancellationToken);

            // Try to send packets
            _threads = new List<Thread>();
            long packetData = 0;
            _totalPacketsCountedByPacketSentEvent = 0;
            _totalPacketsCountedByThread = 0;
            for (int i = 0; i < TOTAL_THREADS; i++)
            {
                if (cancellationToken.IsCancellationRequested)
                    return;

                _threads.Add(new Thread(() =>
                {
                    var tcpClient = _tcpClientBuilder.Create().IPAddress(_ipAddress).Port(11000).Build();
                    tcpClient.PacketSent += (sender, args) =>
                    {
                        Interlocked.Add(ref _totalPacketsCountedByPacketSentEvent, 1);
                    };
                    tcpClient.DataSent += (sender, arg) =>
                    {
                        Interlocked.Add(ref _totalBytesSent, arg.BytesSent);
                    };
                    tcpClient.PacketFailedToSend += (sender, args) =>
                    {
                        Interlocked.Add(ref _totalPacketsFailedToSend, 1);
                    };

                    for (int j = 0; j < TOTAL_PACKETS_PER_THREAD; j++)
                    {
                        Interlocked.Add(ref packetData, 1);
                        var data = Interlocked.Read(ref packetData);

                        int tryRemaining = 10;
                        ClientSendAsyncResult result = ClientSendAsyncResult.CreateDefaultInstance();
                        do
                        {
                            if (cancellationToken.IsCancellationRequested)
                                return;

                            result = tcpClient.SendCommandAsync(0xAA, BitConverter.GetBytes(data), cancellationToken).Result;
                            if (result.IsSent)
                                Interlocked.Increment(ref _totalPacketsCountedByThread);
                        }
                        while (!result.IsSent && tryRemaining-- == 0);
                    }
                }));
            }

            _sw = new Stopwatch();
            _sw.Start();

            _logger.LogInformation("Sending packets ...");

            for (int i = 0; i < TOTAL_THREADS; i++)
            {
                if (cancellationToken.IsCancellationRequested)
                    return;

                _threads[i].Start();
            }

            for (int i = 0; i < TOTAL_THREADS; i++)
            {
                if (cancellationToken.IsCancellationRequested)
                    return;

                _threads[i].Join();
            }

            _sw.Stop();

            _isFinished = true;
        }

        public async Task StopAsync(CancellationToken cancellationToken)
        {
            do
            {
                await Task.Delay(500, CancellationToken.None);
            }
            while (!_threads.Select(x => x.ThreadState == System.Threading.ThreadState.Running).Any());

            if (!_isFinished)
            {
                _logger.LogInformation($"=================================================================================================================");
                _logger.LogError("Task is cancelled.");
            }
            _logger.LogInformation($"=================================================================================================================");

            _logger.LogInformation($"Sent totally {_totalPacketsCountedByThread} packets in {_sw.ElapsedMilliseconds} ms which is counted by _threads.");
            _logger.LogInformation($"Sent totally {_totalPacketsCountedByPacketSentEvent} packets in {_sw.ElapsedMilliseconds} ms which is counted by PacketSentEvent.");
            _logger.LogInformation($"Received totally {_totalPacketsReceivedByListener} packets by listener's PacketReceivedEvent.");
            _logger.LogInformation($"Totally {_totalPacketsFailedToSend} packets failed to send.");
            _logger.LogInformation($"Total bytes sent:\t\t\t\t {_totalBytesSent.ToString("N0")}");
            _logger.LogInformation($"Total bytes received:\t\t\t {_totalBytesReceived.ToString("N0")}");
            _logger.LogInformation($"=================================================================================================================");

            if (_totalPacketsCountedByThread != _totalPacketsCountedByPacketSentEvent)
            {
                _logger.LogError($"_totalPacketsCountedByThread != _totalPacketsCountedByPacketSentEvent => An error found in packets' sent, packets counted by awaited result in _threads ({_totalPacketsCountedByThread}) are not equal to packets counted by PacketSentEvent ({_totalPacketsCountedByPacketSentEvent}).");
                _logger.LogInformation($"=================================================================================================================");
            }

            if (_totalPacketsReceivedByListener != _totalPacketsCountedByThread)
            {
                _logger.LogError($"_totalPacketsReceivedByListener != _totalPacketsCountedByThread => Totally received {_totalPacketsReceivedByListener} packets by listener which is not equal to {_totalPacketsCountedByThread} packets counted by await result in thread.");
                _logger.LogInformation($"=================================================================================================================");
            }

            if (_totalPacketsReceivedByListener != _totalPacketsCountedByPacketSentEvent)
            {
                _logger.LogError($"_totalPacketsReceivedByListener != _totalPacketsCountedByPacketSentEvent => Totally received {_totalPacketsReceivedByListener} packets by listener which is not equal to {_totalPacketsCountedByPacketSentEvent} packets counted by await result in PacketSentEvent.");
                _logger.LogInformation($"=================================================================================================================");
            }
        }
    }
}