namespace TrProtocol.Packets
{
    public struct SpecialFX : IPacket
    {
        public MessageID Type => MessageID.SpecialFX;
        public byte GrowType { get; set; }
        public Point Position { get; set; }
        public byte Height { get; set; }
        public short Gore { get; set; }
    }
}