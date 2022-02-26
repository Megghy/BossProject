namespace TrProtocol.Packets
{
    public struct RequestPassword : IPacket
    {
        public MessageID Type => MessageID.RequestPassword;
    }
}
