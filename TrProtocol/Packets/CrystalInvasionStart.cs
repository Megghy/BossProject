using TrProtocol.Models;

namespace TrProtocol.Packets
{
    public struct CrystalInvasionStart : IPacket
    {
        public MessageID Type => MessageID.CrystalInvasionStart;
        public ShortPosition Position { get; set; }
    }
}