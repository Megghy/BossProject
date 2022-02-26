using TrProtocol.Models;

namespace TrProtocol.Packets
{
    public struct WiredCannonShot : IPacket, IPlayerSlot
    {
        public MessageID Type => MessageID.WiredCannonShot;
        public short Damage { get; set; }
        public float Knockback { get; set; }
        public ShortPosition Position { get; set; }
        public short Angle { get; set; }
        public short Ammo { get; set; }
        public byte PlayerSlot { get; set; }
    }
}