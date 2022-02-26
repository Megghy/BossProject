using TrProtocol.Models;

namespace TrProtocol.Packets
{
    public struct FishOutNPC : IPacket
    {
        public MessageID Type => MessageID.FishOutNPC;
        public UShortPosition Position { get; set; }
        public short Start { get; set; }
    }
}