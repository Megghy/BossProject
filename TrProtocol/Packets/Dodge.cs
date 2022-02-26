namespace TrProtocol.Packets
{
    public struct Dodge : IPacket, IPlayerSlot
    {
        public MessageID Type => MessageID.Dodge;
        public byte PlayerSlot { get; set; }
        public byte DodgeType { get; set; }
    }
}