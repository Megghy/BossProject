namespace TrProtocol.Packets
{
    public struct ClientUUID : IPacket
    {
        public MessageID Type => MessageID.ClientUUID;
        public string UUID { get; set; }
    }
}