namespace TrProtocol.Packets
{
    public struct RequestWorldInfo : IPacket
    {
        public MessageID Type => MessageID.RequestWorldInfo;
    }
}
