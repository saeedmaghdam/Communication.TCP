using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Mabna.Communication.Tcp.Common;
using Mabna.Communication.Tcp.Framework;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Communication.Tcp.Tests
{
    public class Worker : IHostedService
    {
        private readonly ITcpServerBuilder _tcpServerBuilder;
        private readonly ITcpClientBuilder _tcpClientBuilder;
        private readonly ILogger<Worker> _logger;
        private readonly ITcpServer _tcpServer;
        private readonly IPAddress _ipAddress;

        public Worker(ILogger<Worker> logger, ITcpServerBuilder tcpServerBuilder, ITcpClientBuilder tcpClientBuilder)
        {
            _logger = logger;
            _tcpServerBuilder = tcpServerBuilder;
            _tcpClientBuilder = tcpClientBuilder;

            _ipAddress = IPAddress.Parse("127.0.0.1");
            _tcpServer = _tcpServerBuilder.IPAddress(_ipAddress).Port(11000).Build();
            _tcpServer.PacketReceived += (sender, args) =>
            {

                var packet = args.Packet;
                var command = packet.Command.ToArray().DisplayByteArrayAsHex();
                var data = packet.Data.ToArray().DisplayByteArrayAsHex();
            };
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            _tcpServer.StartAsync(cancellationToken);

            // Try to send 10,000 packets
            var threads = new List<Thread>();
            int threadsCount = 100;
            long totalPacketsSentByThreads = 0;
            long packetData = 0;
            for (int i = 0; i < threadsCount; i++)
            {
                threads.Add(new Thread(() =>
                {
                    var tcpClient = _tcpClientBuilder.Create().IPAddress(_ipAddress).Port(11000).Build();
                    tcpClient.PacketSent += (sender, args) =>
                    {
                        Interlocked.Add(ref totalPacketsSentByThreads, 1);
                    };

                    for (int j = 0; j < 100; j++)
                    {
                        Interlocked.Add(ref packetData, 1);
                        var data = Interlocked.Read(ref packetData);

                        int tryRemaining = 10;
                        bool sent = false;
                        do
                        {
                            sent = tcpClient.SendCommandAsync(0xAA, BitConverter.GetBytes(data), cancellationToken).Result;
                            Task.Delay(10, cancellationToken).GetAwaiter().GetResult();
                        }
                        while (!sent && tryRemaining-- == 0);
                    }
                }));
            }

            Stopwatch sw = new Stopwatch();
            sw.Start();

            _logger.LogInformation("Sending packets ...");

            for (int i = 0; i < threadsCount; i++)
                threads[i].Start();

            for (int i = 0; i < threadsCount; i++)
                threads[i].Join();

            sw.Stop();

            _logger.LogInformation($"Sent totally {totalPacketsSentByThreads} packets in {sw.ElapsedMilliseconds} ms.");

            Task.Delay(5000, cancellationToken).GetAwaiter().GetResult();

            // Read received packets
            int totalPackets = 0;
            do
            {
                if (cancellationToken.IsCancellationRequested)
                    break;

                try
                {
                    var command = await _tcpServer.GetCommandAsync(cancellationToken);

                    var commandArray = command.GetBytes().ToArray();
                    if (commandArray.Any())
                        _logger.LogInformation($"{string.Join(" ", BitConverter.ToString(commandArray).Split("-").Select(x => "0x" + x))}\r\nPackets received: {++totalPackets}");
                }
                catch
                {
                    // ignore
                }
            }
            while (true);
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }
    }
}
