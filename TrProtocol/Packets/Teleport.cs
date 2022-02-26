using TrProtocol.Models;

namespace TrProtocol.Packets
{
    public struct Teleport : IPacket, IPlayerSlot
    {
        public MessageID Type => MessageID.Teleport;
        public ProtocolBitsByte Bit1 { get; set; }
        public byte PlayerSlot { get; set; }
        public byte HighBitOfPlayerIsAlwaysZero { get; set; }
        public Vector2 Position { get; set; }
        public byte Style { get; set; }
        [Condition("Bit1", 3)] public int ExtraInfo { get; set; }
    }
}