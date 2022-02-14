using System.IO;

namespace TrProtocol.Models.TileEntities
{
    public partial class ProtocolTELogicSensor : ProtocolTileEntity<TELogicSensor>
    {
        public ProtocolTELogicSensor(TELogicSensor entity) : base(entity)
        {
            LogicCheck = (LogicCheckType)(entity?.logicCheck ?? TELogicSensor.LogicCheckType.None);
            On = entity?.On ?? false;
        }

        public override TileEntityType EntityType => TileEntityType.TELogicSensor;
        public override void WriteExtraData(BinaryWriter writer)
        {
            writer.Write((byte)LogicCheck);
            writer.Write(On);
        }
        public override ProtocolTELogicSensor ReadExtraData(BinaryReader reader)
        {
            LogicCheck = (LogicCheckType)reader.ReadByte();
            On = reader.ReadBoolean();
            return this;
        }

        protected override TELogicSensor ToTrTileEntityInternal()
        {
            return new()
            {
                ID = ID,
                Position = Position,
                logicCheck = (TELogicSensor.LogicCheckType)LogicCheck,
                On = On
            };
        }

        public LogicCheckType LogicCheck { get; set; }
        public bool On { get; set; }
    }
}
