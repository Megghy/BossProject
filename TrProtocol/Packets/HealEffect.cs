namespace TrProtocol.Packets
{
    public struct HealEffect : IPacket, IPlayerSlot
    {
        public MessageID Type => MessageID.HealEffect;
        public byte PlayerSlot { get; set; }
        public short Amount { get; set; }
    }
}