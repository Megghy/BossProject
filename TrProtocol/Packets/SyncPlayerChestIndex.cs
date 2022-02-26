namespace TrProtocol.Packets
{
    public struct SyncPlayerChestIndex : IPacket, IPlayerSlot
    {
        public MessageID Type => MessageID.SyncPlayerChestIndex;
        public byte PlayerSlot { get; set; }
        public short ChestIndex { get; set; }
    }
}