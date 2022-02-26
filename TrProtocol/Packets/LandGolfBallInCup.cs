using TrProtocol.Models;

namespace TrProtocol.Packets
{
    public struct LandGolfBallInCup : IPacket, IOtherPlayerSlot
    {
        public MessageID Type => MessageID.LandGolfBallInCup;
        public byte OtherPlayerSlot { get; set; }
        public UShortPosition Position { get; set; }
        public ushort Hits { get; set; }
        public ushort ProjType { get; set; }
    }
}