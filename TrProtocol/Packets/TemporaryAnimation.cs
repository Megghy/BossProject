using TrProtocol.Models;

namespace TrProtocol.Packets
{
    public struct TemporaryAnimation : IPacket
    {
        public MessageID Type => MessageID.TemporaryAnimation;
        public short AniType { get; set; }
        public short TileType { get; set; }
        public ShortPosition Position { get; set; }
    }
}