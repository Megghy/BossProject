namespace TrProtocol.Packets
{
    public struct SetCountsAsHostForGameplay : IPacket, IOtherPlayerSlot
    {
        public MessageID Type => MessageID.SetCountsAsHostForGameplay;
        public byte OtherPlayerSlot { get; set; }
        public bool Flag { get; set; }
    }
}