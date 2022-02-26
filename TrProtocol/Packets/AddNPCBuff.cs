namespace TrProtocol.Packets
{
    public struct AddNPCBuff : IPacket, INPCSlot
    {
        public MessageID Type => MessageID.AddNPCBuff;
        public short NPCSlot { get; set; }
        [BoundWith("MaxBuffType")]
        public ushort BuffType { get; set; }
        public short BuffTime { get; set; }
    }
}