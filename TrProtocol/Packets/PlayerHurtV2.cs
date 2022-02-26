using TrProtocol.Models;

namespace TrProtocol.Packets
{
    public struct PlayerHurtV2 : IPacket, IOtherPlayerSlot
    {
        public MessageID Type => MessageID.PlayerHurtV2;
        public byte OtherPlayerSlot { get; set; }
        public PlayerDeathReason Reason { get; set; }
        public short Damage { get; set; }
        public byte HitDirection { get; set; }
        public ProtocolBitsByte Bits1 { get; set; }
        public sbyte CoolDown { get; set; }
    }
}