namespace TrProtocol.Packets
{
    public struct ItemOwner : IPacket, IItemSlot, IOtherPlayerSlot
    {
        public MessageID Type => MessageID.ItemOwner;
        public short ItemSlot { get; set; }
        public byte OtherPlayerSlot { get; set; }
    }
}
