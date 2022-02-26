namespace TrProtocol.Packets
{
    public struct Assorted1 : IPacket, IPlayerSlot
    {
        public MessageID Type => MessageID.Assorted1;
        public byte PlayerSlot { get; set; }
        public byte Unknown { get; set; }
    }
}