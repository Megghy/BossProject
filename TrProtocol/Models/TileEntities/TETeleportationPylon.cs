using System.IO;

namespace TrProtocol.Models.TileEntities
{
    public partial class ProtocolTETeleportationPylon : ProtocolTileEntity<TETeleportationPylon>
    {
        public ProtocolTETeleportationPylon(TETeleportationPylon entity) : base(entity)
        {
        }

        public override TileEntityType EntityType => TileEntityType.TETeleportationPylon;

        public override ProtocolTETeleportationPylon ReadExtraData(BinaryReader reader)
        {
            return this;
        }

        public override void WriteExtraData(BinaryWriter writer)
        {
        }

        protected override TETeleportationPylon ToTrTileEntityInternal()
        {
            return new() { ID = ID, Position = Position };
        }
    }
}
