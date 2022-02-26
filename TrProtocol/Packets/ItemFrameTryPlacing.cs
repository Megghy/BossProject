using TrProtocol.Models;

namespace TrProtocol.Packets
{
    public struct ItemFrameTryPlacing : IPacket
    {
        public MessageID Type => MessageID.ItemFrameTryPlacing;
        public ShortPosition Position { get; set; }
        public short ItemType { get; set; }
        public byte Prefix { get; set; }
        public short Stack { get; set; }
    }
}