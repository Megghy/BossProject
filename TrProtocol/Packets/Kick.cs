namespace TrProtocol.Packets
{
    public struct Kick : IPacket
    {
        public MessageID Type => MessageID.Kick;
        public NetworkText Reason { get; set; }
    }
}
