namespace TrProtocol.Packets
{
    public struct PlayerPvP : IPacket, IPlayerSlot
    {
        public MessageID Type => MessageID.PlayerPvP;
        public byte PlayerSlot { get; set; }
        public bool Pvp { get; set; }
    }
}