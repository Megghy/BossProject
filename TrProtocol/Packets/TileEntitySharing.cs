using TrProtocol.Models;

namespace TrProtocol.Packets
{
    public class TileEntitySharing : Packet
    {
        public override MessageID Type => MessageID.TileEntitySharing;
        public int ID { get; set; }
        public bool IsNew { get; set; }
        public TileEntityType EntityType => Entity?.EntityType ?? TileEntityType.Unknown;
        [Condition("IsNew")]
        public IProtocolTileEntity Entity { get; set; }
    }
}