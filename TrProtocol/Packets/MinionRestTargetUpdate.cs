namespace TrProtocol.Packets
{
    public struct MinionRestTargetUpdate : IPacket, IPlayerSlot
    {
        public MessageID Type => MessageID.MinionRestTargetUpdate;
        public byte PlayerSlot { get; set; }
        public Vector2 MinionRestTargetPoint { get; set; }
    }
}