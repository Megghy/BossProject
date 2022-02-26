namespace TrProtocol.Packets
{
    public struct MurderSomeoneElsesProjectile : IPacket, IOtherPlayerSlot
    {
        public MessageID Type => MessageID.MurderSomeoneElsesProjectile;
        public byte OtherPlayerSlot { get; set; }
        public byte HighBitOfPlayerIsAlwaysZero { get; set; }
        public byte AI1 { get; set; }
    }
}