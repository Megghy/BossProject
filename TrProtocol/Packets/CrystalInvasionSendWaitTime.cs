namespace TrProtocol.Packets
{
    public struct CrystalInvasionSendWaitTime : IPacket
    {
        public MessageID Type => MessageID.CrystalInvasionSendWaitTime;
        public int WaitTime { get; set; }
    }
}