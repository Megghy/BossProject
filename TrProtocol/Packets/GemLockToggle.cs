using TrProtocol.Models;

namespace TrProtocol.Packets
{
    public struct GemLockToggle : IPacket
    {
        public MessageID Type => MessageID.GemLockToggle;
        public ShortPosition Position { get; set; }
        public bool Flag { get; set; }
    }
}