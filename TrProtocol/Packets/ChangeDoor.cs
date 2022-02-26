using TrProtocol.Models;

namespace TrProtocol.Packets
{
    public struct ChangeDoor : IPacket
    {
        public MessageID Type => MessageID.ChangeDoor;
        public bool ChangeType { get; set; }
        public ShortPosition Position { get; set; }
        public byte Direction { get; set; }
    }
}
