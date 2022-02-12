using BossFramework;
using BossFramework.DB;
using Bssom.Serializer;
using FakeProvider;
using FreeSql.DataAnnotations;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.DataStructures;
using TrProtocol;
using TrProtocol.Models;
using TrProtocol.Models.TileEntities;
using TrProtocol.Packets;
using TShockAPI;

namespace PlotMarker
{
    internal sealed class Cell : UserConfigBase<Cell>
    {
        public override void Init()
        {
            base.Init();
        }

        #region 信息
        /// <summary>
        /// 所属区域
        /// </summary>
        public long PlotId { get; set; }
        /// <summary> Cell所属的 <see cref="Plot"/> 引用 </summary>

        /// <summary>
        /// 注意 此为相对坐标
        /// </summary>
        public int SpawnX { get; set; } = -1;
        /// <summary>
        /// 注意 此为相对坐标
        /// </summary>
        public int SpawnY { get; set; } = -1;
        /// <summary>
        /// 属地等级, 用于判定大小
        /// </summary>
        public int Level { get; set; } = 1;
        public int LastPositionIndex { get; set; } = -1;
        /// <summary> 属地的主人 </summary>
        public string Owner { get; set; }
        /// <summary>
        /// 玩家 <see cref="Owner"/> 领取属地的时间
        /// </summary>
        public DateTime GetTime { get; set; }
        public DateTime LastAccess { get; set; }
        /// <summary> 有权限动属地者 </summary>
        [JsonMap]
        public List<int> AllowedIDs { get; set; } = new();
        [JsonMap]
        public List<int> UsingCellPosition { get; set; } = new();
        private StructTile?[,] _tileData;
        public StructTile?[,] TileData
        {
            get
            {
                _tileData ??= BssomSerializer.Deserialize<StructTile?[,]>(SerializedTileData);
                return _tileData;
            }
        }
        [Column(DbType = "blob")]
        public byte[] SerializedTileData { get; set; }
        /// <summary>
        /// 注意 其中的坐标为相对坐标
        /// </summary>
        public List<IProtocolTileEntity> Entities
        {
            get
            {
                _entitiesData ??= GetDeserializedEntitiesData();
                return _entitiesData;
            }
        }
        private List<IProtocolTileEntity> _entitiesData;
        [Column(DbType = "blob")]
        public byte[] SerializedEntitiesData { get; set; }
        #endregion

        public Plot Parent => PlotManager.Plots.FirstOrDefault(p => p.Id == PlotId);
        public bool IsVisiable
            => UsingCellPosition.Any();
        public short X
            => (short)(LastPositionIndex == -1
            ? -1
            : Parent.CellsPosition[LastPositionIndex]?.TileX ?? -1);
        public short Y
            => (short)(LastPositionIndex == -1
            ? -1
            : Parent.CellsPosition[LastPositionIndex]?.TileY ?? -1);
        public Microsoft.Xna.Framework.Point Center => new(X + (Parent.CellWidth / 2), Y + (Parent.Height / 2));
        public int Width => Parent.CellWidth * Level + (Parent.LineWidth * Level - 1);
        public int Height => Parent.Height * Level + (Parent.LineWidth * Level - 1);
        public int AbsloteSpawnX
            => LastPositionIndex == -1
            ? -1
            : SpawnY == -1
            ? Center.Y
            : (Parent.CellsPosition[LastPositionIndex]?.TileX ?? -1) + SpawnX;
        public int AbsloteSpawnY
            => LastPositionIndex == -1
            ? -1
            : SpawnY == -1
            ? Center.Y
            : (Parent.CellsPosition[LastPositionIndex]?.TileY ?? -1) + SpawnY;


        private byte[] GetSerializedEntitiesData()
            => Entities.Select(entity => new Tuple<TileEntityType, byte[]>(entity.EntityType, BssomSerializer.Serialize(entity)))
                .SerializeToBytes();
        private List<IProtocolTileEntity> GetDeserializedEntitiesData()
        {
            var list = new List<IProtocolTileEntity>();
            SerializedEntitiesData.DeserializeBytes<Tuple<TileEntityType, byte[]>[]>()
                .ForEach(entityInfo => list.Add(entityInfo.Item1 switch
                {
                    TileEntityType.TEHatRack => entityInfo.Item2.DeserializeBytes<ProtocolTEHatRack>(),
                    TileEntityType.TEWeaponsRack => entityInfo.Item2.DeserializeBytes<ProtocolTEWeaponsRack>(),
                    TileEntityType.TEFoodPlatter => entityInfo.Item2.DeserializeBytes<ProtocolTEFoodPlatter>(),
                    TileEntityType.TEItemFrame => entityInfo.Item2.DeserializeBytes<ProtocolTEItemFrame>(),
                    TileEntityType.TETeleportationPylon => entityInfo.Item2.DeserializeBytes<ProtocolTEItemFrame>(),
                    TileEntityType.TEDisplayDoll => entityInfo.Item2.DeserializeBytes<ProtocolTEDisplayDoll>(),
                    TileEntityType.TETrainingDummy => entityInfo.Item2.DeserializeBytes<ProtocolTETrainingDummy>(),
                    TileEntityType.TELogicSensor => entityInfo.Item2.DeserializeBytes<ProtocolTELogicSensor>(),
                    _ => throw new NotImplementedException()
                }));
            return list;
        }

        public void ClearTiles(bool resetSection = true)
        {
            for (var i = X; i < X + Width; i++)
            {
                for (var j = Y; j < Y + Height; j++)
                {
                    Main.tile[i, j] ??= new StructTile();
                    Main.tile[i, j].ClearEverything();
                }
            }
            if (resetSection)
                TileHelper.ResetSection(X, Y, Width, Height);
        }
        public bool Contains(int x, int y)
        {
            return X <= x && x < X + Parent.CellWidth && Y <= y && y < Y + Parent.CellHeight;
        }
        public string GetInfo()
            => $"属地 {Id} - " +
                $"领主: {(string.IsNullOrWhiteSpace(Owner) ? "无" : Owner)}" +
                $" | 创建: {GetTime:g}" +
                $" | 修改: {LastAccess:g}" +
                $" | 最后一次生成坐标: {X} - {Y}";
        public bool SaveCellData()
        {
            if (!IsVisiable)
                return false;
            //保存世界中的entity
            Entities.Clear();
            var rec = new Rectangle(X, Y, Width, Height);
            TileEntity.ByPosition.Where(t => rec.Contains(t.Key.X, t.Key.Y))
                .ForEach(entity =>
                {
                    var e = Activator.CreateInstance(IProtocolTileEntity.tileEntityDict[(TileEntityType)entity.Value.type], new object[] { entity.Value }) as IProtocolTileEntity;
                    e.Position = new(e.Position.X - X, e.Position.X - X); //转换为相对坐标
                });
            SerializedEntitiesData = GetSerializedEntitiesData();

            //保存物块数据
            var startX = X;
            var startY = Y;
            for (int x = 0; x < TileData.GetLength(0); x++)
            {
                for (int y = 0; y < TileData.GetLength(1); y++)
                {
                    TileData[x, y]?.CopyFrom(Main.tile[startX + x, startY + y]);
                }
            }
            SerializedTileData = TileData.SerializeToBytes();

            return UpdateMany(c => c.SerializedEntitiesData
                , c => c.SerializedTileData) == 1;
        }
        public void Invisiable(bool resetSection = true)
        {
            if (!IsVisiable)
                return;
            UsingCellPosition.Clear();
            ClearTiles(false); //这个不需要发section

            //删除地图上的entity
            List<Packet> packetData = new();
            Entities.ForEach(entity =>
            {
                if (TileEntity.ByID.TryGetValue(entity.ID, out var placedEntity))
                {
                    packetData.Add(new TileEntitySharing()
                    {
                        ID = placedEntity.ID,
                        IsNew = false,
                    });
                }
            });
            BInfo.OnlinePlayers.ForEach(p => p.SendPackets(packetData));

            Parent.ReDrawLines(resetSection);
        }
        public bool ShowCell(TSPlayer plr)
        {
            if (IsVisiable)
                return false;
            if (this.FindUseableArea() is { } pos)
            {
                LastPositionIndex = pos.First().Index;
                UsingCellPosition.AddRange(pos.Select(p => p.Index));

                //还原物块
                var startX = X;
                var startY = Y;
                for (int x = 0; x < TileData.GetLength(0); x++)
                {
                    for (int y = 0; y < TileData.GetLength(1); y++)
                    {
                        if (TileData[x, y].HasValue)
                            Main.tile[startX + x, startY + y]?.CopyFrom(TileData[x, y]);
                        else
                            Main.tile[startX + x, startY + y].ClearEverything();
                    }
                }
                //还原entity
                List<Packet> packetData = new(); //合并数据包 减少发包次数
                Entities.ForEach(entity =>
                {
                    var entityX = startX + entity.Position.X;
                    var entityY = startY + entity.Position.Y;
                    TileEntity.PlaceEntityNet((int)entity.EntityType, entityX, entityY);
                    if (TileEntity.ByPosition.TryGetValue(new(startX, startY), out var placedEntity))
                    {
                        //更新原有信息
                        entity.Position = new(entityX, entityY);
                        entity.ID = placedEntity.ID; 
                        packetData.Add(new TileEntitySharing()
                        {
                            ID = placedEntity.ID,
                            IsNew = true,
                            Entity = entity
                        });
                    }
                });

                TileHelper.ResetSection(startX, startY, Width, Height);
                BInfo.OnlinePlayers.ForEach(p => p.SendPackets(packetData));
                return true;
            }
            else
                plr.SendInfoMessage($"无法生成属地区块, 请联系管理员");
            return false;
        }
    }
}
