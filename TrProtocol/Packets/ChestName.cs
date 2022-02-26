using TrProtocol.Models;

namespace TrProtocol.Packets
{
    public struct ChestName : IPacket
    {
        public MessageID Type => MessageID.ChestName;
        public short ChestSlot { get; set; }
        public ShortPosition Position { get; set; }
        public byte NameLength { get; set; }
        private bool ShouldReadName => NameLength is > 0 and <= 20;
        [Condition("ShouldReadName")]
        public string Name { get; set; }
    }
}