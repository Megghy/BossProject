using System.IO;

namespace TrProtocol.Models.TileEntities
{
    public partial class ProtocolTEWeaponsRack : ProtocolTileEntity<TEWeaponsRack>
    {
        public ProtocolTEWeaponsRack(TEWeaponsRack entity) : base(entity)
        {
            Item = entity.item;
        }

        public override TileEntityType EntityType => TileEntityType.TEWeaponsRack;
        public override void WriteExtraData(BinaryWriter writer)
        {
            Item.Write(writer);
        }

        public override ProtocolTEWeaponsRack ReadExtraData(BinaryReader reader)
        {
            Item = new(reader);
            return this;
        }

        protected override TEWeaponsRack ToTrTileEntityInternal()
        {
            return new()
            {
                ID = ID,
                Position = Position,
                item = Item
            };
        }

        public ItemData Item { get; set; }
    }
}
