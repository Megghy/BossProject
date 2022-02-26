namespace TrProtocol.Packets
{
    public struct Count : IPacket
    {
        public MessageID Type => MessageID.Count;
    }
}