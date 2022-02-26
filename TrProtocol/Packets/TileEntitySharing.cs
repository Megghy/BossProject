using TrProtocol.Models;

namespace TrProtocol.Packets
{
    public struct TileEntitySharing : IPacket
    {
        public MessageID Type => MessageID.TileEntitySharing;
        public int ID { get; set; }
        public bool IsNew { get; set; }
        public TileEntityType EntityType => Entity?.EntityType ?? TileEntityType.Unknown;
        [Condition("IsNew")]
        public IProtocolTileEntity Entity { get; set; }
    }
}