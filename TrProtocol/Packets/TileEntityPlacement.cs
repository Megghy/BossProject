using TrProtocol.Models;

namespace TrProtocol.Packets
{
    public struct TileEntityPlacement : IPacket
    {
        public MessageID Type => MessageID.TileEntityPlacement;
        public ShortPosition Position { get; set; }
        public byte TileEntityType { get; set; }
    }
}