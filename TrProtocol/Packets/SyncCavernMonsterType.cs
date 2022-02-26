namespace TrProtocol.Packets
{
    public struct SyncCavernMonsterType : IPacket
    {
        public MessageID Type => MessageID.SyncCavernMonsterType;
        [ArraySize(6)]
        public short[] CavenMonsterType { get; set; }
    }
}