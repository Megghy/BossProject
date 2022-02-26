namespace TrProtocol.Packets
{
    public struct ToggleParty : IPacket
    {
        public MessageID Type => MessageID.ToggleParty;
    }
}