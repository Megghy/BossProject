using TrProtocol.Models;

namespace TrProtocol.Packets
{
    public struct SyncPlayerChest : IPacket
    {
        public MessageID Type => MessageID.SyncPlayerChest;
        public short ChestSlot { get; set; }
        public ShortPosition Position { get; set; }
        public byte ChestNameLength { get; set; }
        private bool _shouldGetName => ChestNameLength < 21 && ChestNameLength > 0;
        [Condition("_shouldGetName")]
        public string ChestName { get; set; }
    }
}