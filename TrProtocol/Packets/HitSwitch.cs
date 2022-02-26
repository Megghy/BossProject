using TrProtocol.Models;

namespace TrProtocol.Packets
{
    public struct HitSwitch : IPacket
    {
        public MessageID Type => MessageID.HitSwitch;
        public ShortPosition Position { get; set; }
    }
}