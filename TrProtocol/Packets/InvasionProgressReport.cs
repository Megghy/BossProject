namespace TrProtocol.Packets
{
    public struct InvasionProgressReport : IPacket
    {
        public MessageID Type => MessageID.InvasionProgressReport;
        public int Progress { get; set; }
        public int ProgressMax { get; set; }
        public sbyte Icon { get; set; }
        public sbyte Wave { get; set; }
    }
}