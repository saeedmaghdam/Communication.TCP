namespace Mabna.Communication.Tcp.Framework
{
    public class CommandOptions
    {
        public bool AckRequired
        {
            get;
            private set;
        }

        public bool ResponseRequired
        {
            get;
            private set;
        }

        public static bool TryParse(byte value, out CommandOptions result)
        {
            var temp = new CommandOptions();

            temp.AckRequired = (value & 0b0000_0001) > 0;
            temp.ResponseRequired = (value & 0b0000_0010) > 0;

            result = temp;
            return true;
        }
    }
}
