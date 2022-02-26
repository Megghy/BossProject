namespace TrProtocol.Packets
{
    public struct LoadPlayer : IPacket, IPlayerSlot
    {
        public MessageID Type => MessageID.LoadPlayer;
        public byte PlayerSlot { get; set; }
        public bool ServerWantsToRunCheckBytesInClientLoopThread { get; set; }
    }
}
