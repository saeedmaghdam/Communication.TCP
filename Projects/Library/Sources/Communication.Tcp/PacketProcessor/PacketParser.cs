using System;
using System.Linq;
using Mabna.Communication.Tcp.Common;
using Mabna.Communication.Tcp.Framework;
using Microsoft.Extensions.Logging;

namespace Mabna.Communication.Tcp.PacketProcessor
{
    public class PacketParser : IPacketParser
    {
        private readonly ILogger<PacketParser> _logger;

        public PacketParser(ILogger<PacketParser> logger)
        {
            _logger = logger;
        }

        public bool TryParse(PacketConfig packetConfig, byte[] bytes, out PacketModel packetModel)
        {
            var activity = ActivityHelper.Start();

            packetModel = new PacketModel(null, null, null, null, null, null, null);

            int headerStartIndex,
                dataSizeStartIndex,
                commandStartIndex,
                commandOptionsStartIndex,
                dataStartIndex,
                crcStartIndex,
                tailStartIndex,
                headerTotalBytes,
                dataSizeTotalBytes,
                commandTotalBytes,
                commandOptionsTotalBytes,
                dataTotalBytes,
                crcTotalBytes,
                tailTotalBytes;

            if (packetConfig == null)
            {
                _logger.LogError("Packet processor is not initialized.");
                activity.Stop();
                throw new Exception("Packet processor is not initialized.");
            }

            var start = 0;
            var size = packetConfig.Header.Length;
            var header = bytes.ReadBytes(start, size);
            if (!header.SequenceEqual(packetConfig.Header))
            {
                _logger.LogWarning("Packet parser failed at header matching step.");
                activity.Stop();
                return false;
            }
            headerStartIndex = start;
            headerTotalBytes = size;

            start += size;
            size = packetConfig.DataMaxSize;
            var dataSize = bytes.ReadBytes(start, size).ToArray();
            if (dataSize.Length < packetConfig.DataMaxSize)
            {
                _logger.LogWarning("Packet parser failed at data-size matching step.");
                activity.Stop();
                return false;
            }
            dataSizeStartIndex = start;
            dataSizeTotalBytes = size;

            start += size;
            size = packetConfig.CommandBytesSize;
            var command = bytes.ReadBytes(start, size);
            commandStartIndex = start;
            commandTotalBytes = size;

            start += size;
            size = packetConfig.CommandOptionsBytesSize;
            var commandOptions = bytes.ReadBytes(start, size);
            commandOptionsStartIndex = start;
            commandOptionsTotalBytes = size;

            start += size;
            size = BitConverter.ToInt32(dataSize, 0);
            var data = bytes.ReadBytes(start, size);
            dataStartIndex = start;
            dataTotalBytes = size;

            start += size;
            size = 1;
            var crc = bytes.ReadBytes(start, size);
            var calculatedCrc = Util.CalculateCRC(dataSize, command, commandOptions, data);
            if (!crc.SequenceEqual(calculatedCrc))
            {
                _logger.LogWarning("Packet parser failed at CRC calculation step.");
                activity.Stop();
                return false;
            }
            crcStartIndex = start;
            crcTotalBytes = size;

            start += size;
            size = packetConfig.Tail.Length;
            var tail = bytes.ReadBytes(start, size);
            if (!tail.SequenceEqual(packetConfig.Tail))
            {
                _logger.LogWarning("Packet parser failed at tail matching step.");
                activity.Stop();
                return false;
            }
            tailStartIndex = start;
            tailTotalBytes = size;

            packetModel = new PacketModel(bytes.ReadBytes(headerStartIndex, headerTotalBytes), bytes.ReadBytes(dataSizeStartIndex, dataSizeTotalBytes), bytes.ReadBytes(commandStartIndex, commandTotalBytes), bytes.ReadBytes(commandOptionsStartIndex, commandOptionsTotalBytes), bytes.ReadBytes(dataStartIndex, dataTotalBytes), bytes.ReadBytes(crcStartIndex, crcTotalBytes), bytes.ReadBytes(tailStartIndex, tailTotalBytes));

            _logger.LogTrace("Packet parsed successfully.");
            activity.Stop();
            return true;
        }

        public bool TryParse(PacketConfig packetConfig, byte[] bytes, int bytesReceived, out PacketModel packetModel)
        {
            var activity = ActivityHelper.Start();

            packetModel = new PacketModel(null, null, null, null, null, null, null);

            int headerStartIndex,
                dataSizeStartIndex,
                commandStartIndex,
                commandOptionsStartIndex,
                dataStartIndex,
                crcStartIndex,
                tailStartIndex,
                headerTotalBytes,
                dataSizeTotalBytes,
                commandTotalBytes,
                commandOptionsTotalBytes,
                dataTotalBytes,
                crcTotalBytes,
                tailTotalBytes;

            if (packetConfig == null)
            {
                _logger.LogError("Packet processor is not initialized.");
                activity.Stop();
                throw new Exception("Packet processor is not initialized.");
            }

            var start = bytesReceived - packetConfig.Tail.Length;
            var size = packetConfig.Tail.Length;
            var tail = bytes.ReadBytes(start, size);
            if (!tail.SequenceEqual(packetConfig.Tail))
            {
                _logger.LogWarning("Packet parser failed at tail matching step.");
                activity.Stop();
                return false;
            }
            tailStartIndex = start;
            tailTotalBytes = size;

            start = 0;
            size = packetConfig.Header.Length;
            var header = bytes.ReadBytes(start, size);
            if (!header.SequenceEqual(packetConfig.Header))
            {
                _logger.LogWarning("Packet parser failed at header matching step.");
                activity.Stop();
                return false;
            }
            headerStartIndex = start;
            headerTotalBytes = size;

            start += size;
            size = packetConfig.DataMaxSize;
            var dataSize = bytes.ReadBytes(start, size).ToArray();
            if (dataSize.Length < packetConfig.DataMaxSize)
            {
                _logger.LogWarning("Packet parser failed at data-size matching step.");
                activity.Stop();
                return false;
            }
            dataSizeStartIndex = start;
            dataSizeTotalBytes = size;

            start += size;
            size = packetConfig.CommandBytesSize;
            var command = bytes.ReadBytes(start, size);
            commandStartIndex = start;
            commandTotalBytes = size;

            start += size;
            size = packetConfig.CommandOptionsBytesSize;
            var commandOptions = bytes.ReadBytes(start, size);
            commandOptionsStartIndex = start;
            commandOptionsTotalBytes = size;

            start += size;
            size = BitConverter.ToInt32(dataSize, 0);
            var data = bytes.ReadBytes(start, size);
            dataStartIndex = start;
            dataTotalBytes = size;

            start += size;
            size = 1;
            var crc = bytes.ReadBytes(start, size);
            var calculatedCrc = Util.CalculateCRC(dataSize, command, commandOptions, data);
            if (!crc.SequenceEqual(calculatedCrc))
            {
                _logger.LogWarning("Packet parser failed at CRC calculation step.");
                activity.Stop();
                return false;
            }
            crcStartIndex = start;
            crcTotalBytes = size;

            packetModel = new PacketModel(bytes.ReadBytes(headerStartIndex, headerTotalBytes), bytes.ReadBytes(dataSizeStartIndex, dataSizeTotalBytes), bytes.ReadBytes(commandStartIndex, commandTotalBytes), bytes.ReadBytes(commandOptionsStartIndex, commandOptionsTotalBytes), bytes.ReadBytes(dataStartIndex, dataTotalBytes), bytes.ReadBytes(crcStartIndex, crcTotalBytes), bytes.ReadBytes(tailStartIndex, tailTotalBytes));

            _logger.LogTrace("Packet parsed successfully.");
            activity.Stop();
            return true;
        }
    }
}
