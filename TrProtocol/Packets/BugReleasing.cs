namespace TrProtocol.Packets
{
    public struct BugReleasing : IPacket
    {
        public MessageID Type => MessageID.BugReleasing;
        public Point Position { get; set; }
        public short NPCType { get; set; }
        public byte Style { get; set; }
    }
}