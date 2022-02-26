namespace TrProtocol.Packets
{
    public struct MinionAttackTargetUpdate : IPacket, IPlayerSlot
    {
        public MessageID Type => MessageID.MinionAttackTargetUpdate;
        public byte PlayerSlot { get; set; }
        public short MinionAttackTarget { get; set; }
    }
}