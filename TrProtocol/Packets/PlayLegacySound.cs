using TrProtocol.Models;

namespace TrProtocol.Packets
{
    public struct PlayLegacySound : IPacket
    {
        public MessageID Type => MessageID.PlayLegacySound;
        public Vector2 Point { get; set; }
        public ushort Sound { get; set; }
        public ProtocolBitsByte Bits1 { get; set; }
        [Condition("Bits1", 0)]
        public int Style { get; set; }
        [Condition("Bits1", 1)]
        public float Volume { get; set; }
        [Condition("Bits1", 2)]
        public float Pitch { get; set; }
    }
}