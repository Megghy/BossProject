using TrProtocol.Models;

namespace TrProtocol.Packets
{
    public struct NPCHome : IPacket, INPCSlot
    {
        public MessageID Type => MessageID.NPCHome;
        public short NPCSlot { get; set; }
        public ShortPosition Position { get; set; }
        public byte Homeless { get; set; }
    }
}