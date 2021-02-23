namespace Mabna.Communication.Tcp.Framework
{
    public class AckCommand : PacketModel
    {
        public AckCommand(PacketConfig packetConfig) : base(packetConfig.Header, new byte[packetConfig.DataMaxSize], new byte[] { 0x00 }, new byte[] { }, new byte[] { 0x00 }, packetConfig.Tail)
        {
        }
    }
}
