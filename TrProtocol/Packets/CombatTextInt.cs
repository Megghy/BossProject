namespace TrProtocol.Packets
{
    public struct CombatTextInt : IPacket
    {
        public MessageID Type => MessageID.CombatTextInt;
        public Vector2 Position { get; set; }
        public Color Color { get; set; }
        public int Amount { get; set; }
    }
}