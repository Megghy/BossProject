using TrProtocol.Models;

namespace TrProtocol.Packets
{
    public struct RequestReadSign : IPacket
    {
        public MessageID Type => MessageID.RequestReadSign;
        public ShortPosition Position { get; set; }
    }
}