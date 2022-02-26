using TrProtocol.Models;

namespace TrProtocol.Packets
{
    public struct TileChange : IPacket
    {
        public MessageID Type => MessageID.TileChange;
        public byte ChangeType { get; set; }
        public ShortPosition Position { get; set; }
        [BoundWith("MaxTileType")]
        public short TileType { get; set; }
        public byte Style { get; set; }
    }
}
