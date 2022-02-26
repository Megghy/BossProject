namespace TrProtocol.Packets
{
    public struct TeleportNPCThroughPortal : IPacket, INPCSlot
    {
        public MessageID Type => MessageID.TeleportNPCThroughPortal;
        public short NPCSlot { get; set; }
        public ushort Extra { get; set; }
        public Vector2 Position { get; set; }
        public Vector2 Velocity { get; set; }
    }
}