namespace Mabna.Communication.Tcp.Framework
{
    public interface ICommandOptionsBuilder
    {
        // Bits definition:
        // Bit 0    =   Ack Required
        // Bit 1    =   Response Required
        // Bit 2    =   *RESERVED
        // Bit 3    =   *RESERVED
        // Bit 4    =   *RESERVED
        // Bit 5    =   *RESERVED
        // Bit 6    =   *RESERVED
        // Bit 7    =   *RESERVED

        ICommandOptionsBuilder AckRequired(bool value);

        ICommandOptionsBuilder ResponseRequired(bool value);

        byte Build();
    }
}
