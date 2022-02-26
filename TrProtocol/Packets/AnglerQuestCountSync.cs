namespace TrProtocol.Packets
{
    public struct AnglerQuestCountSync : IPacket, IPlayerSlot
    {
        public MessageID Type => MessageID.AnglerQuestCountSync;
        public byte PlayerSlot { get; set; }
        public int AnglerQuestsFinished { get; set; }
        public int GolferScoreAccumulated { get; set; }
    }
}