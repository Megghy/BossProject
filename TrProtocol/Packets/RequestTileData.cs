using TrProtocol.Models;

namespace TrProtocol.Packets
{
    public class RequestTileData : Packet
    {
        public override MessageID Type => MessageID.RequestTileData;
        public Point Position { get; set; }
    }
}
