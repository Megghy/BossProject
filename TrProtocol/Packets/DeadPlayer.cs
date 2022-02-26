namespace TrProtocol.Packets
{
    public struct DeadPlayer : IPacket, IOtherPlayerSlot
    {
        public MessageID Type => MessageID.DeadPlayer;
        public byte OtherPlayerSlot { get; set; }
    }
}