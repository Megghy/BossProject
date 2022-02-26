namespace TrProtocol.Packets
{
    public struct TileCounts : IPacket
    {
        public MessageID Type => MessageID.TileCounts;
        public byte Good { get; set; }
        public byte Evil { get; set; }
        public byte Blood { get; set; }
    }
}