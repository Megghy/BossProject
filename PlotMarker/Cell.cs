using BossFramework;
using BossFramework.BCore;
using BossFramework.DB;
using FakeProvider;
using FreeSql.DataAnnotations;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.GameContent.Tile_Entities;
using TrProtocol;
using TrProtocol.Models;
using TrProtocol.Packets;
using TShockAPI;
using IProtocolTileEntity = TrProtocol.Models.TileEntity;
using TileEntity = Terraria.DataStructures.TileEntity;

namespace PlotMarker
{
    struct SimpleChestData
    {
        public string Name { get; set; }
        public short TileX { get; set; }
        public short TileY { get; set; }
        public TrProtocol.Models.ItemData[] Items { get; set; }
    }
    struct SimpleSignData
    {
        public short TileX { get; set; }
        public short TileY { get; set; }
        public string Text { get; set; }
    }

    internal sealed class Cell : UserConfigBase<Cell>
    {
        public static readonly Dictionary<TileEntityType, Type> tileEntityDict = new()
        {
            { TileEntityType.TETrainingDummy, typeof(TETrainingDummy) },
            { TileEntityType.TEItemFrame, typeof(TEItemFrame) },
            { TileEntityType.TELogicSensor, typeof(TELogicSensor) },
            { TileEntityType.TEDisplayDoll, typeof(TEDisplayDoll) },
            { TileEntityType.TEWeaponsRack, typeof(TEWeaponsRack) },
            { TileEntityType.TEHatRack, typeof(TEHatRack) },
            { TileEntityType.TEFoodPlatter, typeof(TEFoodPlatter) },
            { TileEntityType.TETeleportationPylon, typeof(TETeleportationPylon) }
        };
        public override void Init()
        {
            if (SerializedEntitiesData != null)
                Entities = GetDeserializedEntitiesData();
            if (SerializedTileData != null)
                TileData = GetDeserializedTildData();
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
        public List<int> UsingCellPositionIndex { get; set; } = new();
        public StructTile[,] TileData { get; set; }
        [Column(DbType = "MEDIUMBLOB")]
        public byte[] SerializedTileData { get; set; }
        /// <summary>
        /// 注意 其中的坐标为相对坐标
        /// </summary>
        [Column(DbType = "MEDIUMBLOB")]
        public byte[] SerializedEntitiesData { get; set; }
        public List<IProtocolTileEntity> Entities { get; set; } = new();
        [JsonMap]
        public List<SimpleSignData> CellSigns { get; set; } = new();
        [JsonMap]
        public List<SimpleChestData> CellChests { get; set; } = new();
        #endregion

        public Plot Parent => PlotManager.Plots.FirstOrDefault(p => p.Id == PlotId);
        public bool IsVisiable
            => UsingCellPositionIndex.Any();
        public short X
            => (short)(LastPositionIndex == -1
            ? -1
            : Parent.CellsPosition[LastPositionIndex]?.TileX ?? -1);
        public short Y
            => (short)(LastPositionIndex == -1
            ? -1
            : Parent.CellsPosition[LastPositionIndex]?.TileY ?? -1);
        public Point Center => new(X + (Width / 2), Y + (Height / 2));
        public int Width => Parent.CellWidth * Level + (Parent.LineWidth * (Level - 1));
        public int Height => Parent.CellHeight * Level + (Parent.LineWidth * (Level - 1));
        public int AbsloteSpawnX
            => LastPositionIndex == -1
            ? -1
            : SpawnX == -1
            ? Center.X
            : X + SpawnX;
        public int AbsloteSpawnY
            => LastPositionIndex == -1
            ? -1
            : SpawnY == -1
            ? Center.Y
            : Y + SpawnY;

        internal byte[] GetSerializedTileData()
            => TileData.SerializeToBytes().CompressBytes();
        internal StructTile[,] GetDeserializedTildData()
            => SerializedTileData is null
            ? new StructTile[Width, Height]
            : SerializedTileData.DecompressBytes().DeserializeBytes<StructTile[,]>();
        internal byte[] GetSerializedEntitiesData()
            => Entities.Select(entity => new TempSavedEntityData() { EntityData = entity.SerializeToBytes(), Type = entity.EntityType })
                .ToArray()
                .SerializeToBytes()
                .CompressBytes();
        internal List<IProtocolTileEntity> GetDeserializedEntitiesData()
        {
            var list = new List<IProtocolTileEntity>();
            if (SerializedEntitiesData is null)
                return list;
            SerializedEntitiesData.DecompressBytes().DeserializeBytes<TempSavedEntityData[]>()
                .TForEach(entityInfo => list.Add(entityInfo.Type switch
                {
                    TrProtocol.Models.TileEntityType.TEHatRack => entityInfo.EntityData.DeserializeBytes<TrProtocol.Models.TileEntities.TEHatRack>(),
                    TrProtocol.Models.TileEntityType.TEWeaponsRack => entityInfo.EntityData.DeserializeBytes<TrProtocol.Models.TileEntities.TEWeaponsRack>(),
                    TrProtocol.Models.TileEntityType.TEFoodPlatter => entityInfo.EntityData.DeserializeBytes<TrProtocol.Models.TileEntities.TEFoodPlatter>(),
                    TrProtocol.Models.TileEntityType.TEItemFrame => entityInfo.EntityData.DeserializeBytes<TrProtocol.Models.TileEntities.TEItemFrame>(),
                    TrProtocol.Models.TileEntityType.TETeleportationPylon => entityInfo.EntityData.DeserializeBytes<TrProtocol.Models.TileEntities.TEItemFrame>(),
                    TrProtocol.Models.TileEntityType.TEDisplayDoll => entityInfo.EntityData.DeserializeBytes<TrProtocol.Models.TileEntities.TEDisplayDoll>(),
                    TrProtocol.Models.TileEntityType.TETrainingDummy => entityInfo.EntityData.DeserializeBytes<TrProtocol.Models.TileEntities.TETrainingDummy>(),
                    TrProtocol.Models.TileEntityType.TELogicSensor => entityInfo.EntityData.DeserializeBytes<TrProtocol.Models.TileEntities.TELogicSensor>(),
                    _ => throw new NotImplementedException()
                }));
            return list;
        }

        public void ClearTiles(bool sendSection = true)
        {
            for (var i = X; i < X + Width; i++)
            {
                for (var j = Y; j < Y + Height; j++)
                {
                    Main.tile[i, j] ??= new StructTile();
                    Main.tile[i, j].ClearEverything();
                }
            }
            if (sendSection)
                TileHelper.ResetSection(X, Y, Width, Height);
        }
        public bool Contains(int x, int y)
        {
            return X <= x && x < X + Parent.CellWidth && Y <= y && y < Y + Parent.CellHeight;
        }
        public string GetInfo()
            => $"ID: [{Id}] <{(IsVisiable ? "可见" : "不可见")}> - " +
                $"领主: {(string.IsNullOrWhiteSpace(Owner) ? "未知" : Owner)}" +
                $" | 创建: {GetTime:g}" +
                $" | 修改: {LastAccess:g}" +
                $" | 最后一次生成坐标: {X} - {Y}" +
                $" | 箱子数量: {CellChests.Count}" +
                $" | 牌子数量: {CellSigns.Count}" +
                $" | 物块数量: {(TileData ?? GetDeserializedTildData()).To1D().Count(t => !t.isTheSameAs(StructTile.Empty))}";

        public bool SaveCellData(bool force = false)
        {
            if (!IsVisiable && !force)
                return false;

            short startX = X;
            short startY = Y;

            //保存世界中的entity
            Entities.Clear();
            var rec = new Rectangle(startX, startY, Width, Height);
            TileEntity.ByPosition.Where(t => rec.Contains(t.Key.X, t.Key.Y))
                .TForEach(entity =>
                {
                    var e = entity.Value.ToProtocalTileEntity();
                    e.Position = new((short)(entity.Key.X - startX), (short)(entity.Key.Y - startY)); //转换为相对坐标
                    Entities.Add(e);
                });
            SerializedEntitiesData = GetSerializedEntitiesData();

            //保存物块数据
            for (int x = 0; x < TileData.GetLength(0); x++)
            {
                for (int y = 0; y < TileData.GetLength(1); y++)
                {
                    TileData[x, y].CopyFrom(Main.tile[startX + x, startY + y]);
                }
            }
            SerializedTileData = GetSerializedTileData();

            //保存箱子数据
            ChestRedirector.AllChest().Where(c => rec.Contains(c.X, c.Y))
                .TForEach(c =>
                {
                    if (CellChests.FirstOrDefault(s => s.TileX == c.X - startX && s.TileY == c.Y - startY) is { } oldChest)
                        oldChest.Items = c.Items;
                    else
                        CellChests.Add(new()
                        {
                            TileX = (short)(c.X - startX), //注意转换为相对坐标
                            TileY = (short)(c.Y - startY),
                            Items = c.Items
                        });
                });

            //保存牌子数据
            SignRedirector.AllSign().Where(s => rec.Contains(s.X, s.Y))
                .TForEach(s =>
                {
                    if (CellSigns.FirstOrDefault(temp => temp.TileX == s.X - startX && temp.TileY == s.Y - startY) is { } oldSign)
                        oldSign.Text = s.Text;
                    else
                        CellSigns.Add(new()
                        {
                            TileX = (short)(s.X - startX), //注意转换为相对坐标
                            TileY = (short)(s.Y - startY),
                            Text = s.Text
                        });
                });

            return DBTools.SQL.Update<Cell>(this)
                .Set(c => c.Level, Level)
                .Set(c => c.Owner, Owner)
                .Set(c => c.AllowedIDs, AllowedIDs)
                .Set(c => c.UsingCellPositionIndex, UsingCellPositionIndex)
                .Set(c => c.LastPositionIndex, LastPositionIndex)
                .Set(c => c.LastAccess, LastAccess)
                .Set(c => c.SerializedEntitiesData, SerializedEntitiesData)
                .Set(c => c.SerializedTileData, SerializedTileData)
                .Set(c => c.CellSigns, CellSigns)
                .Set(c => c.CellChests, CellChests)
                .ExecuteAffrows() == 1;
        }

        #region 管理

        #region 隐藏
        public void Invisiable(bool sendSection = true, bool saveData = true)
        {
            if (!IsVisiable)
                return;

            UsingCellPositionIndex.Clear(); //移除占用的位置

            if (saveData)
                SaveCellData(true); //先保存数据

            ClearTiles(false); //清除物块, 这里不需要同步section

            //删除地图上的entity
            ClearEntities();

            //移除箱子牌子信息
            DeregisteChestAndSign();

            //重新画线
            //Parent.ReDrawLines(sendSection);
            //不升级的话就不用重画线

            //清理内存占用
            Entities = null;
            TileData = null;

            if (sendSection)
                TileHelper.ResetSection(X, Y, Width, Height);

            BLog.Info($"属地 [{Id}]<{Owner}> 现在处于隐藏状态");
        }
        public void ClearEntities()
        {
            List<Packet> packetData = new();
            Entities.ForEach(entity =>
            {
                if (TileEntity.ByID.TryGetValue(entity.ID, out var placedEntity))
                {
                    TileEntity.ByID.Remove(placedEntity.ID);
                    TileEntity.ByPosition.Remove(placedEntity.Position);

                    if (placedEntity is TETrainingDummy dummy && dummy.npc != -1)
                    {
                        NPC npc = Main.npc[dummy.npc];
                        npc.type = 0;
                        npc.active = false;
                        NetMessage.SendData((int)PacketTypes.NpcUpdate, -1, -1, null, dummy.npc);
                        dummy.npc = -1;
                        (entity as TrProtocol.Models.TileEntities.TETrainingDummy).NPC = -1;
                    }

                    packetData.Add(new TileEntitySharing()
                    {
                        ID = placedEntity.ID,
                        IsNew = false,
                    });
                }
            });
            BUtils.SendPacketsToAll(packetData);
        }
        public void DeregisteChestAndSign()
        {
            //删除游戏地图上的箱子
            CellChests.ForEach(c =>
            {
                ChestRedirector.RemoveChest(c.TileX + X, c.TileY + Y); //移除新生成的
                ChestRedirector.DeregisterOverrideChest(c.TileX + X, c.TileY + Y); //移除注册的
            });

            //移除地图上的牌子
            CellSigns.ForEach(s =>
            {
                SignRedirector.RemoveSign(s.TileX + X, s.TileY + Y); //移除新生成的
                SignRedirector.DeregisterOverrideSign(s.TileX + X, s.TileY + Y); //移除注册的
            });
        }
        #endregion

        #region 展示
        public void ReDraw()
        {
            TileData ??= GetDeserializedTildData();
            Entities ??= GetDeserializedEntitiesData();

            //还原物块
            RestoreCellTileData(false);

            //还原entity
            RestoreEntities();

            //还原箱子牌子
            RegisteChestAndSign();

            TileHelper.ResetSection(X, Y, Width, Height); //发送更改后的区块数据
        }
        public bool Visiable(TSPlayer plr = null, bool force = false)
        {
            if (IsVisiable && !force)
                return false;
            if (this.FindUseableArea() is { } pos)
            {
                LastPositionIndex = pos.Index;
                UsingCellPositionIndex.Clear();
                UsingCellPositionIndex.Add(pos.Index);

                UpdateSingle(c => c.LastPositionIndex);
                UpdateSingle(c => c.UsingCellPositionIndex);

                ReDraw();

                BLog.Info($"属地 [{Id}]<{Owner}> 现在处于展示状态");

                return true;
            }
            else
                plr?.SendInfoMessage($"无法生成属地区块, 请联系管理员");
            return false;
        }

        public void RestoreCellTileData(bool sendSection = true)
        {
            var cellX = X;
            var cellY = Y;
            for (int x = 0; x < TileData.GetLength(0); x++)
            {
                for (int y = 0; y < TileData.GetLength(1); y++)
                {
                    Main.tile[cellX + x, cellY + y]?.CopyFrom(TileData[x, y]);
                }
            }
            if (sendSection)
                TileHelper.ResetSection(X, Y, Width, Height); //发送更改后的区块数据
        }

        public void RegisteChestAndSign()
        {
            //还原箱子
            CellChests.ForEach(c => ChestRedirector.RegisterOverrideChest((short)(c.TileX + X), (short)(c.TileY + Y), c.Items));

            //还原牌子
            CellSigns.ForEach(c => SignRedirector.RegisterOverrideSign((short)(c.TileX + X), (short)(c.TileY + Y), c.Text));
        }

        public void RestoreEntities(bool reScanEntity = true)
        {
            List<byte> packetData = new(); //合并数据包 减少发包次数
            Entities.ForEach(entity =>
            {
                var entityX = X + entity.Position.X;
                var entityY = Y + entity.Position.Y;
                TileEntity.ByPosition.Where(t => t.Key.X == entityX && t.Key.Y == entityY)
                .ToArray()
                .TForEach(e =>
                {
                    TileEntity.ByPosition.Remove(e.Key);
                    TileEntity.ByID.Remove(e.Value.ID);
                    packetData.AddRange(new TileEntitySharing()
                    {
                        ID = e.Value.ID,
                        IsNew = false
                    }.SerializePacket());
                });
                TileEntity.manager._types[(int)entity.EntityType].NetPlaceEntityAttempt(entityX, entityY);
                if (TileEntity.ByPosition.TryGetValue(new(entityX, entityY), out var placedEntity))
                {
                    var trEntity = entity.ToTrTileEntity();
                    trEntity.ID = placedEntity.ID;
                    trEntity.Position = new(placedEntity.Position.X, placedEntity.Position.Y);
                    TileEntity.ByID[placedEntity.ID] = trEntity;
                    TileEntity.ByPosition[placedEntity.Position] = trEntity;

                    entity.ID = placedEntity.ID;
                    entity.Position = new((short)entityX, (short)entityY); //位置信息为绝对坐标
                    packetData.AddRange(new TileEntitySharing()
                    {
                        ID = placedEntity.ID,
                        IsNew = true,
                        Entity = entity
                    }.SerializePacket());
                    entity.Position = new((short)(entityX - X), (short)(entityY - Y)); //更换位置信息为相对坐标
                }
            });
            if (reScanEntity)
                FakeProviderAPI.World.ScanEntities(); //fakeprovider重新获取entity
            BInfo.OnlinePlayers.TForEach(p => p.SendRawData(packetData.ToArray()));
        }
        #endregion

        #endregion
    }
}
