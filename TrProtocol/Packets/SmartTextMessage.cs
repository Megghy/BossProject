namespace TrProtocol.Packets
{
    public struct SmartTextMessage : IPacket
    {
        public MessageID Type => MessageID.SmartTextMessage;
        public Color Color { get; set; }
        public NetworkText Text { get; set; }
        public short Width { get; set; }
    }
}
