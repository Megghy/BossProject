namespace TrProtocol.Packets
{
    public struct StartPlaying : IPacket
    {
        public MessageID Type => MessageID.StartPlaying;
    }
}
