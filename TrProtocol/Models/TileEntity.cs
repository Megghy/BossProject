using System;
using System.Collections.Generic;
using System.IO;
using Terraria.DataStructures;
using TrProtocol.Models.TileEntities;

namespace TrProtocol.Models
{
    public interface IProtocolTileEntity
    {
        public static readonly IReadOnlyDictionary<TileEntityType, Type> tileEntityDict = new Dictionary<TileEntityType, Type>()
        {
            { TileEntityType.TETrainingDummy, typeof(ProtocolTETrainingDummy) },
            { TileEntityType.TEItemFrame, typeof(ProtocolTEItemFrame) },
            { TileEntityType.TELogicSensor, typeof(ProtocolTELogicSensor) },
            { TileEntityType.TEDisplayDoll, typeof(ProtocolTEDisplayDoll) },
            { TileEntityType.TEWeaponsRack, typeof(ProtocolTEWeaponsRack) },
            { TileEntityType.TEHatRack, typeof(ProtocolTEHatRack) },
            { TileEntityType.TEFoodPlatter, typeof(ProtocolTEFoodPlatter) },
            { TileEntityType.TETeleportationPylon, typeof(ProtocolTETeleportationPylon) }
        };
        public static IProtocolTileEntity Read(BinaryReader br)
        {
            var type = (TileEntityType)br.ReadByte();
            if (tileEntityDict.TryGetValue(type, out var t))
            {
                var entity = Activator.CreateInstance(t) as IProtocolTileEntity;
                entity.ID = br.ReadInt32();
                entity.Position = new() { X = br.ReadInt16(), Y = br.ReadInt16() };
                entity.ReadExtraData(br);
                return entity;
            }
            else
                return null;
        }
        public static void Write(BinaryWriter bw, IProtocolTileEntity t)
        {
            bw.Write((byte)t.EntityType);
            bw.Write(t.ID);
            bw.Write(t.Position.X);
            bw.Write(t.Position.Y);
            t.WriteExtraData(bw);
        }
        public TileEntityType EntityType { get; }
        public ShortPosition Position { get; set; }
        public int ID { get; set; }
        public void WriteExtraData(BinaryWriter writer);
        public IProtocolTileEntity ReadExtraData(BinaryReader reader);
        public TileEntity ToTrTileEntity();
    }
    public abstract partial class ProtocolTileEntity<T> : IProtocolTileEntity where T : TileEntity
    {
        public ProtocolTileEntity(T entity)
        {
            Position = entity.Position;
            ID = entity.ID;
        }
        public abstract TileEntityType EntityType { get; }
        public ShortPosition Position { get; set; }
        public int ID { get; set; }
        public abstract void WriteExtraData(BinaryWriter writer);
        public abstract IProtocolTileEntity ReadExtraData(BinaryReader reader);
        protected abstract T ToTrTileEntityInternal();
        public TileEntity ToTrTileEntity() => ToTrTileEntity();
    }
}
