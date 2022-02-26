namespace TrProtocol.Packets
{
    public struct AddPlayerBuff : IPacket, IOtherPlayerSlot
    {
        public MessageID Type => MessageID.AddPlayerBuff;
        public byte OtherPlayerSlot { get; set; }
        [BoundWith("MaxBuffType")]
        public ushort BuffType { get; set; }
        public int BuffTime { get; set; }
    }
}