namespace TrProtocol.Packets
{
    public struct AchievementMessageNPCKilled : IPacket
    {
        public MessageID Type => MessageID.AchievementMessageNPCKilled;
        public short NPCType { get; set; }
    }
}