using TrProtocol.Models;

namespace TrProtocol.Packets
{
    public struct TileSquare : IPacket
    {
        public MessageID Type => MessageID.TileSquare;
        public SquareData Data { get; set; }
    }
}
