namespace TrProtocol.Packets
{
    public struct PlayerActive : IPacket, IPlayerSlot
    {
        public MessageID Type => MessageID.PlayerActive;
        public byte PlayerSlot { get; set; }
        public bool Active { get; set; }
    }
}
