namespace TrProtocol.Packets
{
    public struct FinishedConnectingToServer : IPacket
    {
        public MessageID Type => MessageID.FinishedConnectingToServer;
    }
}