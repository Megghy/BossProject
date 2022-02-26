namespace TrProtocol.Packets
{
    public struct ClientHello : IPacket
    {
        public MessageID Type => MessageID.ClientHello;
        public string Version { get; set; }
    }
}
