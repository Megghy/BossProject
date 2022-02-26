namespace TrProtocol.Packets
{
    public struct SpawnBoss : IPacket, IOtherPlayerSlot
    {
        public MessageID Type => MessageID.SpawnBoss;
        public byte OtherPlayerSlot { get; set; }
        public byte HighBitOfPlayerIsAlwaysZero { get; set; }
        public short NPCType { get; set; }
    }
}