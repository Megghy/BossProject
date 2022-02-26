using TrProtocol.Models;

namespace TrProtocol.Packets
{
    public struct RequestChestOpen : IPacket
    {
        public MessageID Type => MessageID.RequestChestOpen;
        public ShortPosition Position { get; set; }
    }
}