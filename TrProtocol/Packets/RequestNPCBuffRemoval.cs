namespace TrProtocol.Packets
{
    public struct RequestNPCBuffRemoval : IPacket, INPCSlot
    {
        public MessageID Type => MessageID.RequestNPCBuffRemoval;
        public short NPCSlot { get; set; }
        public ushort BuffType { get; set; }
    }
}