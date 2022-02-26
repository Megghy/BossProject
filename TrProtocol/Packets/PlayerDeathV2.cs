using TrProtocol.Models;

namespace TrProtocol.Packets
{
    public struct PlayerDeathV2 : IPacket, IPlayerSlot
    {
        public MessageID Type => MessageID.PlayerDeathV2;
        public byte PlayerSlot { get; set; }
        public PlayerDeathReason Reason { get; set; }
        public short Damage { get; set; }
        public byte HitDirection { get; set; }
        public ProtocolBitsByte Bits1 { get; set; }
    }
}