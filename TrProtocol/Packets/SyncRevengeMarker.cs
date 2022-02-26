namespace TrProtocol.Packets
{
    public struct SyncRevengeMarker : IPacket
    {
        public MessageID Type => MessageID.SyncRevengeMarker;
        public int ID { get; set; }
        public Vector2 Position { get; set; }
        public int NetID { get; set; }
        public float Percent { get; set; }
        public int NPCType { get; set; }
        public int NPCAI { get; set; }
        public int CoinValue { get; set; }
        public float BaseValue { get; set; }
        public bool SpawnFromStatue { get; set; }
    }
}