using TrProtocol.Models;

namespace TrProtocol.Packets
{
    public struct Unlock : IPacket, IPlayerSlot
    {
        public MessageID Type => MessageID.Unlock;
        public byte PlayerSlot { get; set; }
        public ShortPosition Position { get; set; }
    }
}