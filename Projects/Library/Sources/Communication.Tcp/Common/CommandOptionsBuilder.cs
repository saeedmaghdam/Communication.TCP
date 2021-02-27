using Mabna.Communication.Tcp.Framework;

namespace Mabna.Communication.Tcp.Common
{
    public class CommandOptionsBuilder : ICommandOptionsBuilder
    {
        private bool _ackRequired;
        private bool _responseRequired;

        public CommandOptionsBuilder() { }

        public ICommandOptionsBuilder AckRequired()
        {
            _ackRequired = true;

            return this;
        }

        public ICommandOptionsBuilder ResponseRequired()
        {
            _responseRequired = true;

            return this;
        }

        public byte Build()
        {
            byte result = 0x00;

            if (_ackRequired)
                result = (byte)(result | 0b0000_0001);

            if (_responseRequired)
                result = (byte)(result | 0b0000_0010);

            return result;
        }
    }
}
