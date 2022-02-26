namespace TrProtocol.Packets
{
    public struct PlayerTeam : IPacket, IPlayerSlot
    {
        public MessageID Type => MessageID.PlayerTeam;
        public byte PlayerSlot { get; set; }
        public byte Team { get; set; }
    }
}