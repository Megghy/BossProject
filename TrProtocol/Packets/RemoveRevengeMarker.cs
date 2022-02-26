namespace TrProtocol.Packets
{
    public struct RemoveRevengeMarker : IPacket
    {
        public MessageID Type => MessageID.RemoveRevengeMarker;
        public int ID { get; set; }
    }
}