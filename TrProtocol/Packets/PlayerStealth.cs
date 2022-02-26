namespace TrProtocol.Packets
{
    public struct PlayerStealth : IPacket, IPlayerSlot
    {
        public MessageID Type => MessageID.PlayerStealth;
        public byte PlayerSlot { get; set; }
        public float Stealth { get; set; }
    }
}