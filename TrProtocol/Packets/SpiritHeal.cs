namespace TrProtocol.Packets
{
    public struct SpiritHeal : IPacket, IOtherPlayerSlot
    {
        public MessageID Type => MessageID.SpiritHeal;
        public byte OtherPlayerSlot { get; set; }
        public short Amount { get; set; }
    }
}