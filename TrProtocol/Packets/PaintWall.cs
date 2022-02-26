using TrProtocol.Models;

namespace TrProtocol.Packets
{
    public struct PaintWall : IPacket
    {
        public MessageID Type => MessageID.PaintWall;
        public ShortPosition Position { get; set; }
        public byte Color { get; set; }
    }
}