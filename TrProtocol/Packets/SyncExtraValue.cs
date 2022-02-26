namespace TrProtocol.Packets
{
    public struct SyncExtraValue : IPacket
    {
        public MessageID Type => MessageID.SyncExtraValue;
        public short NPCSlot { get; set; }
        public int Extra { get; set; }
        public Vector2 MoneyPing { get; set; }
    }
}