namespace TrProtocol.Packets
{
    public struct ResetItemOwner : IPacket, IItemSlot
    {
        public MessageID Type => MessageID.ResetItemOwner;
        public short ItemSlot { get; set; }
    }
}