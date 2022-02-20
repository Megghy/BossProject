using System.IO;

namespace TrProtocol.Models.TileEntities
{
    public partial class ProtocolTEItemFrame : ProtocolTileEntity<TEItemFrame>
    {
        public ProtocolTEItemFrame(TEItemFrame entity) : base(entity)
        {
            Item = entity?.item ?? new();
        }

        public override TileEntityType EntityType => TileEntityType.TEItemFrame;
        public override void WriteExtraData(BinaryWriter writer)
        {
            Item ??= new();
            Item.Write(writer);
        }

        public override ProtocolTEItemFrame ReadExtraData(BinaryBufferReader reader)
        {
            Item = new(reader);
            return this;
        }

        protected override TEItemFrame ToTrTileEntityInternal()
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
