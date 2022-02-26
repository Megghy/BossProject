namespace TrProtocol.Packets
{
    public struct NPCKillCountDeathTally : IPacket
    {
        public MessageID Type => MessageID.NPCKillCountDeathTally;
        public short NPCType { get; set; }
        public int Count { get; set; }
    }
}