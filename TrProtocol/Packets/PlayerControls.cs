using TrProtocol.Models;

namespace TrProtocol.Packets
{
    public struct PlayerControls : IPacket, IPlayerSlot
    {
        public MessageID Type => MessageID.PlayerControls;
        public byte PlayerSlot { get; set; }
        public ProtocolBitsByte Bit1 { get; set; }
        public ProtocolBitsByte Bit2 { get; set; }
        public ProtocolBitsByte Bit3 { get; set; }
        public ProtocolBitsByte Bit4 { get; set; }
        public byte SelectedItem { get; set; }
        public Vector2 Position { get; set; }
        [Condition("Bit2", 2)]
        public Vector2 Velocity { get; set; }
        [Condition("Bit3", 6)]
        public Vector2 PotionOfReturnOriginalUsePosition { get; set; }
        [Condition("Bit3", 6)]
        public Vector2 PotionOfReturnHomePosition { get; set; }
    }
}
