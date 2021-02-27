using System;
using System.Linq;
using Mabna.Communication.Tcp.Common;
using Mabna.Communication.Tcp.Framework;

namespace Mabna.Communication.Tcp.PacketProcessor
{
    public class PacketParser : IPacketParser
    {
        public bool TryParse(PacketConfig packetConfig, byte[] bytes, out PacketModel packetModel)
        {
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
                throw new Exception("Packet processor is not initialized.");

            var start = 0;
            var size = packetConfig.Header.Length;
            var header = bytes.ReadBytes(start, size);
            if (!header.SequenceEqual(packetConfig.Header))
                return false;
            headerStartIndex = start;
            headerTotalBytes = size;

            start += size;
            size = packetConfig.DataMaxSize;
            var dataSize = bytes.ReadBytes(start, size).ToArray();
            if (dataSize.Length < packetConfig.DataMaxSize)
                return false;
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
                return false;
            crcStartIndex = start;
            crcTotalBytes = size;

            start += size;
            size = packetConfig.Tail.Length;
            var tail = bytes.ReadBytes(start, size);
            if (!tail.SequenceEqual(packetConfig.Tail))
                return false;
            tailStartIndex = start;
            tailTotalBytes = size;

            packetModel = new PacketModel(bytes.ReadBytes(headerStartIndex, headerTotalBytes), bytes.ReadBytes(dataSizeStartIndex, dataSizeTotalBytes), bytes.ReadBytes(commandStartIndex, commandTotalBytes), bytes.ReadBytes(commandOptionsStartIndex, commandOptionsTotalBytes), bytes.ReadBytes(dataStartIndex, dataTotalBytes), bytes.ReadBytes(crcStartIndex, crcTotalBytes), bytes.ReadBytes(tailStartIndex, tailTotalBytes));

            return true;
        }

        public bool TryParse(PacketConfig packetConfig, byte[] bytes, int bytesReceived, out PacketModel packetModel)
        {
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
                throw new Exception("Packet processor is not initialized.");

            var start = bytesReceived - packetConfig.Tail.Length;
            var size = packetConfig.Tail.Length;
            var tail = bytes.ReadBytes(start, size);
            if (!tail.SequenceEqual(packetConfig.Tail))
                return false;
            tailStartIndex = start;
            tailTotalBytes = size;

            start = 0;
            size = packetConfig.Header.Length;
            var header = bytes.ReadBytes(start, size);
            if (!header.SequenceEqual(packetConfig.Header))
                return false;
            headerStartIndex = start;
            headerTotalBytes = size;

            start += size;
            size = packetConfig.DataMaxSize;
            var dataSize = bytes.ReadBytes(start, size).ToArray();
            if (dataSize.Length < packetConfig.DataMaxSize)
                return false;
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
            size = BitConverter.ToInt16(dataSize, 0);
            var data = bytes.ReadBytes(start, size);
            dataStartIndex = start;
            dataTotalBytes = size;

            start += size;
            size = 1;
            var crc = bytes.ReadBytes(start, size);
            var calculatedCrc = Util.CalculateCRC(dataSize, command, commandOptions, data);
            if (!crc.SequenceEqual(calculatedCrc))
                return false;
            crcStartIndex = start;
            crcTotalBytes = size;
            
            packetModel = new PacketModel(bytes.ReadBytes(headerStartIndex, headerTotalBytes), bytes.ReadBytes(dataSizeStartIndex, dataSizeTotalBytes), bytes.ReadBytes(commandStartIndex, commandTotalBytes), bytes.ReadBytes(commandOptionsStartIndex, commandOptionsTotalBytes), bytes.ReadBytes(dataStartIndex, dataTotalBytes), bytes.ReadBytes(crcStartIndex, crcTotalBytes), bytes.ReadBytes(tailStartIndex, tailTotalBytes));

            return true;
        }
    }
}
