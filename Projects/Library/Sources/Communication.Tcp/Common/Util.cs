using System;
using System.Collections.Generic;
using System.Linq;

namespace Mabna.Communication.Tcp.Common
{
    public static class Util
    {
        public static byte[] CalculateCRC(params IEnumerable<byte>[] bytesCollection)
        {
            byte[] result = new byte[1];

            foreach (var bytes in bytesCollection)
            {
                foreach (var b in bytes)
                {
                    result[0] ^= b;
                }
            }

            return result;
        }

        public static string DisplayByteArrayAsHex(this byte[] data)
        {
            return string.Join(" ", BitConverter.ToString(data).Split("-").Select(x => "0x" + x));
        }
    }
}
