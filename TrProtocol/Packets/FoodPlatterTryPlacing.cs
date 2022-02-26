using TrProtocol.Models;

namespace TrProtocol.Packets
{
    public struct FoodPlatterTryPlacing : IPacket
    {
        public MessageID Type => MessageID.FoodPlatterTryPlacing;
        public ShortPosition Position { get; set; }
        public short ItemType { get; set; }
        public byte Prefix { get; set; }
        public short Stack { get; set; }
    }
}