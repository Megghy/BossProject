using TrProtocol.Models;

namespace TrProtocol.Packets
{
    public struct ChestUpdates : IPacket
    {
        public MessageID Type => MessageID.ChestUpdates;
        public byte Operation { get; set; }
        public ShortPosition Position { get; set; }
        public short Style { get; set; }
        public short ChestSlot { get; set; }
    }
}