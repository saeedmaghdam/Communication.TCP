using System.Collections.Generic;
using System.Net.Sockets;
using System.Threading.Tasks;
using Mabna.Communication.Tcp.Framework;

namespace Mabna.Communication.Tcp.Common
{
    public static class Extensions
    {
        public static IEnumerable<byte> ReadBytes(this byte[] bytes, int start, int size)
        {
            var end = start + size;
            if (end > bytes.Length)
                end = bytes.Length;

            for (var i = start; i < end; i++)
                yield return bytes[i];
        }

        public static IEnumerable<byte> ReadTailBytes(this byte[] bytes, int tailsSize)
        {
            var bytesLength = bytes.Length - tailsSize;
            if (bytesLength < 0)
                yield break;
            
            foreach (var b in bytes.ReadBytes(bytesLength, tailsSize))
                yield return b;
        }

        public static IEnumerable<byte> ReadHeaderBytes(this byte[] bytes, int headerSize)
        {
            var bytesLength = bytes.Length - headerSize;
            if (bytesLength < 0)
                yield break;

            foreach (var b in bytes.ReadBytes(0, headerSize))
                yield return b;
        }
    }
}
