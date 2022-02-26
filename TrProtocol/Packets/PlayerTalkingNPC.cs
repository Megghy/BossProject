namespace TrProtocol.Packets
{
    public struct PlayerTalkingNPC : IPacket, IPlayerSlot, INPCSlot
    {
        public MessageID Type => MessageID.PlayerTalkingNPC;
        public byte PlayerSlot { get; set; }
        public short NPCSlot { get; set; }
    }
}