namespace TrProtocol.Packets
{
    public struct PoofOfSmoke : IPacket
    {
        public MessageID Type => MessageID.PoofOfSmoke;
        public uint PackedHalfVector2 { get; set; }
    }
}