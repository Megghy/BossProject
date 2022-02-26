using TrProtocol.Models;

namespace TrProtocol.Packets
{
    public struct MassWireOperation : IPacket
    {
        public MessageID Type => MessageID.MassWireOperation;
        public ShortPosition Start { get; set; }
        public ShortPosition End { get; set; }
        public MultiToolMode Mode { get; set; }
    }
}