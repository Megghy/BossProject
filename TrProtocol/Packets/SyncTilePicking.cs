using TrProtocol.Models;

namespace TrProtocol.Packets
{
    public struct SyncTilePicking : IPacket, IPlayerSlot
    {
        public MessageID Type => MessageID.SyncTilePicking;
        public byte PlayerSlot { get; set; }
        public ShortPosition Position { get; set; }
        public byte Damage { get; set; }
    }
}