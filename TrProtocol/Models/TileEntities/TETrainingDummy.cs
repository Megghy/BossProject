using System.IO;

namespace TrProtocol.Models.TileEntities
{
    public partial class ProtocolTETrainingDummy : ProtocolTileEntity<TETrainingDummy>
    {
        public ProtocolTETrainingDummy(TETrainingDummy entity) : base(entity)
        {
            NPC = entity?.npc ?? -1;
        }

        public override TileEntityType EntityType => TileEntityType.TETrainingDummy;
        public override void WriteExtraData(BinaryWriter writer)
        {
            writer.Write((short)NPC);
        }

        public override ProtocolTETrainingDummy ReadExtraData(BinaryReader reader)
        {
            NPC = reader.ReadInt16();
            return this;
        }

        protected override TETrainingDummy ToTrTileEntityInternal()
        {
            return new()
            {
                ID = ID,
                Position = Position,
                npc = NPC
            };
        }

        public int NPC { get; set; }
    }
}
