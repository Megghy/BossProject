namespace TrProtocol.Packets
{
    public struct SetMiscEventValues : IPacket, IOtherPlayerSlot
    {
        public MessageID Type => MessageID.SetMiscEventValues;
        public byte OtherPlayerSlot { get; set; }
        public int CreditsRollTime { get; set; }
    }
}