namespace TrProtocol.Packets
{
    public struct AnglerQuest : IPacket
    {
        public MessageID Type => MessageID.AnglerQuest;
        public byte QuestType { get; set; }
        public bool Finished { get; set; }
    }
}