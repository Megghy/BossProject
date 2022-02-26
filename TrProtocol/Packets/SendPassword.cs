namespace TrProtocol.Packets
{
    public struct SendPassword : IPacket
    {
        public MessageID Type => MessageID.SendPassword;
        public string Password { get; set; }
    }
}
