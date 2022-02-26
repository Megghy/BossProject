using TrProtocol.Models;

namespace TrProtocol.Packets
{
    public struct FrameSection : IPacket
    {
        public MessageID Type => MessageID.FrameSection;
        public ShortPosition Start { get; set; }
        public ShortPosition End { get; set; }
    }
}
