namespace TrProtocol.Packets
{
    public struct PlayerBuffs : IPacket, IPlayerSlot
    {
        public MessageID Type => MessageID.PlayerBuffs;
        public byte PlayerSlot { get; set; }
        [ArraySize(22)]
        public ushort[] BuffTypes { get; set; }
    }
}