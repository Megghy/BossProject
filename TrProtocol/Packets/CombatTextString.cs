namespace TrProtocol.Packets
{
    public struct CombatTextString : IPacket
    {
        public MessageID Type => MessageID.CombatTextString;
        public Vector2 Position { get; set; }
        public Color Color { get; set; }
        public NetworkText Text { get; set; }
    }
}