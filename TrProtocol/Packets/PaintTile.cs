using TrProtocol.Models;

namespace TrProtocol.Packets
{
    public struct PaintTile : IPacket
    {
        public MessageID Type => MessageID.PaintTile;
        public ShortPosition Position { get; set; }
        public byte Color { get; set; }
    }
}