﻿using System.Collections.Generic;
using System.Linq;

namespace Mabna.Communication.Tcp.Framework
{
    public class PacketModel
    {
        public IEnumerable<byte> Header
        {
            get;
        }

        public IEnumerable<byte> DataSize
        {
            get;
        }

        public IEnumerable<byte> Command
        {
            get;
        }

        public IEnumerable<byte> Data
        {
            get;
        }

        public IEnumerable<byte> Crc
        {
            get;
        }

        public IEnumerable<byte> Tail
        {
            get;
        }
        
        public PacketModel(IEnumerable<byte> header, IEnumerable<byte> dataSize, IEnumerable<byte> command, IEnumerable<byte> data, IEnumerable<byte> crc, IEnumerable<byte> tail)
        {
            Header = header;
            DataSize = dataSize;
            Command = command;
            Data = data;
            Crc = crc;
            Tail = tail;
        }

        public IEnumerable<byte> GetBytes()
        {
            return Header.Concat(DataSize).Concat(Command).Concat(Data).Concat(Crc).Concat(Tail);
        }
    }
}
