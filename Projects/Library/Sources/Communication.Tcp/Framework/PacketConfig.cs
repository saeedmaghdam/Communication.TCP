namespace Mabna.Communication.Tcp.Framework
{
    public class PacketConfig
    {
        public byte[] Header
        {
            get;
        }

        public byte[] Tail
        {
            get;
        }

        public int CommandBytesSize
        {
            get;
        }

        public int CommandOptionsBytesSize
        {
            get;
        }

        public int DataMaxSize
        {
            get;
        }

        public PacketConfig(byte[] header, byte[] tail)
        {
            Header = header;
            Tail = tail;

            Header ??= new byte[]
            {
                0x24, 0x4d, 0x3e
            };

            Tail ??= new byte[]
            {
                0x0d, 0x0a
            };

            CommandBytesSize = 1;

            CommandOptionsBytesSize = 1;

            DataMaxSize = 4;
        }
    }
}
