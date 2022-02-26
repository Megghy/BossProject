namespace TrProtocol.Packets
{
    public struct ManaEffect : IPacket, IPlayerSlot
    {
        public MessageID Type => MessageID.ManaEffect;
        public byte PlayerSlot { get; set; }
        public short Amount { get; set; }
    }
}