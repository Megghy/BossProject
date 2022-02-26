namespace TrProtocol.Packets
{
    public struct AchievementMessageEventHappened : IPacket
    {
        public MessageID Type => MessageID.AchievementMessageEventHappened;
        public short EventType { get; set; }
    }
}