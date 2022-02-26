namespace TrProtocol.Packets
{
    public struct StrikeNPC : IPacket, INPCSlot
    {
        public MessageID Type => MessageID.StrikeNPC;
        public short NPCSlot { get; set; }
        public short Damage { get; set; }
        public float Knockback { get; set; }
        public byte HitDirection { get; set; }
        public bool Crit { get; set; }
    }
}