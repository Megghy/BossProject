namespace TrProtocol.Packets
{
    public struct PlayerMana : IPacket, IPlayerSlot
    {
        public MessageID Type => MessageID.PlayerMana;
        public byte PlayerSlot { get; set; }
        public short StatMana { get; set; }
        public short StatManaMax { get; set; }
    }
}