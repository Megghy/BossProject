using TrProtocol.Models;

namespace TrProtocol.Packets
{
    public struct PlaceObject : IPacket
    {
        public MessageID Type => MessageID.PlaceObject;
        public ShortPosition Position { get; set; }
        public short ObjectType { get; set; }
        public short Style { get; set; }
        public byte Alternate { get; set; }
        public sbyte Random { get; set; }
        public bool Direction { get; set; }
    }
}