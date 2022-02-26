namespace TrProtocol.Packets
{
    public struct SocialHandshake : IPacket
    {
        public MessageID Type => MessageID.SocialHandshake;
        public byte[] Raw { get; set; }
    }
}