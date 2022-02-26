namespace TrProtocol.Packets
{
    public struct MoonlordCountdown : IPacket
    {
        public MessageID Type => MessageID.MoonlordCountdown;
        public int Countdown { get; set; }
    }
}