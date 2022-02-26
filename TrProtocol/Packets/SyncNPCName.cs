namespace TrProtocol.Packets
{
    public struct SyncNPCName : IPacket, INPCSlot
    {
        public MessageID Type => MessageID.SyncNPCName;
        public short NPCSlot { get; set; }
        [S2COnly]
        public string NPCName { get; set; }
        [S2COnly]
        public int TownNpc { get; set; }
    }
}