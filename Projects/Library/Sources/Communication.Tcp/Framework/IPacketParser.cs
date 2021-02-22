namespace Mabna.Communication.Tcp.Framework
{
    public interface IPacketParser
    {
        bool TryParse(PacketConfig packetConfig, byte[] bytes, out PacketModel packetModel);
        bool TryParse(PacketConfig packetConfig, byte[] bytes, int bytesReceived, out PacketModel packetModel);
    }
}
