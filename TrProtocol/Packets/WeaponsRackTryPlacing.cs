using TrProtocol.Models;

namespace TrProtocol.Packets
{
    public struct WeaponsRackTryPlacing : IPacket
    {
        public MessageID Type => MessageID.WeaponsRackTryPlacing;
        public ShortPosition Position { get; set; }
        public short ItemType { get; set; }
        public byte Prefix { get; set; }
        public short Stack { get; set; }
    }
}