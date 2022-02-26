using TrProtocol.Models;
namespace TrProtocol.Packets
{
    public struct TileSection : IPacket
    {
        public MessageID Type => MessageID.TileSection;
        public SectionData Data { get; set; }
    }
}
