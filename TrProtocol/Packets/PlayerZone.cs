namespace TrProtocol.Packets
{
    public struct PlayerZone : IPacket, IPlayerSlot
    {
        public MessageID Type => MessageID.PlayerZone;
        public byte PlayerSlot { get; set; }
        public int Zone { get; set; }
    }
}