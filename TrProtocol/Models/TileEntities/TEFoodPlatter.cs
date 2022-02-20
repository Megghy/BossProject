using System.IO;

namespace TrProtocol.Models.TileEntities
{
    public partial class ProtocolTEFoodPlatter : ProtocolTileEntity<TEFoodPlatter>
    {
        public ProtocolTEFoodPlatter(TEFoodPlatter entity) : base(entity)
        {
            Item = entity?.item ?? new();
        }

        public override void WriteExtraData(BinaryWriter writer)
        {
            Item.Write(writer);
        }

        public override ProtocolTEFoodPlatter ReadExtraData(BinaryBufferReader reader)
        {
            Item = new ItemData(reader);
            return this;
        }

        protected override TEFoodPlatter ToTrTileEntityInternal()
        {
            return new()
            {
                item = Item,
                Position = Position
            };
        }

        public override TileEntityType EntityType => TileEntityType.TEFoodPlatter;
        public ItemData Item { get; set; }
    }
}
