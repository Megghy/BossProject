namespace TrProtocol.Packets
{
    public struct TeleportationPotion : IPacket
    {
        public MessageID Type => MessageID.TeleportationPotion;
        public byte Style { get; set; }
    }
}