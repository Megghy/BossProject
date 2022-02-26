namespace TrProtocol.Packets
{
    public struct QuickStackChests : IPacket
    {
        public MessageID Type => MessageID.QuickStackChests;
        public byte ChestSlot { get; set; }
    }
}