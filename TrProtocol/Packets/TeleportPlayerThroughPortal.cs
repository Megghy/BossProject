namespace TrProtocol.Packets
{
    public struct TeleportPlayerThroughPortal : IPacket, IOtherPlayerSlot
    {
        public MessageID Type => MessageID.TeleportPlayerThroughPortal;
        public byte OtherPlayerSlot { get; set; }
        public ushort Extra { get; set; }
        public Vector2 Position { get; set; }
        public Vector2 Velocity { get; set; }
    }
}