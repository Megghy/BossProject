﻿using BossFramework;
using BossFramework.BCore;
using BossFramework.DB;
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
    record SimpleChestData
    {
        public string Name { get; set; }
        public short TileX { get; set; }
        public short TileY { get; set; }
        public ItemData[] Items { get; set; } = new ItemData[40];
    }
    record SimpleSignData
    {
        public short TileX { get; set; }
        public short TileY { get; set; }
        public string Text { get; set; }
    }
    internal sealed class Cell : UserConfigBase<Cell>
    {
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
        public List<IProtocolTileEntity> Entities { get; set; }
        [Column(DbType = "MEDIUMBLOB")]
        public byte[] SerializedEntitiesData { get; set; }
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
        public Point Center => new(X + (Parent.CellWidth / 2), Y + (Parent.Height / 2));
        public int Width => Parent.CellWidth * Level + (Parent.LineWidth * Level - 1);
        public int Height => Parent.Height * Level + (Parent.LineWidth * Level - 1);
        public int AbsloteSpawnX
            => LastPositionIndex == -1
            ? -1
            : SpawnX == -1
            ? Center.X
            : (Parent.CellsPosition[LastPositionIndex]?.TileX ?? -1) + SpawnX;
        public int AbsloteSpawnY
            => LastPositionIndex == -1
            ? -1
            : SpawnY == -1
            ? Center.Y
            : (Parent.CellsPosition[LastPositionIndex]?.TileY ?? -1) + SpawnY;

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
                .ForEach(entityInfo => list.Add(entityInfo.Type switch
                {
                    TileEntityType.TEHatRack => entityInfo.EntityData.DeserializeBytes<ProtocolTEHatRack>(),
                    TileEntityType.TEWeaponsRack => entityInfo.EntityData.DeserializeBytes<ProtocolTEWeaponsRack>(),
                    TileEntityType.TEFoodPlatter => entityInfo.EntityData.DeserializeBytes<ProtocolTEFoodPlatter>(),
                    TileEntityType.TEItemFrame => entityInfo.EntityData.DeserializeBytes<ProtocolTEItemFrame>(),
                    TileEntityType.TETeleportationPylon => entityInfo.EntityData.DeserializeBytes<ProtocolTEItemFrame>(),
                    TileEntityType.TEDisplayDoll => entityInfo.EntityData.DeserializeBytes<ProtocolTEDisplayDoll>(),
                    TileEntityType.TETrainingDummy => entityInfo.EntityData.DeserializeBytes<ProtocolTETrainingDummy>(),
                    TileEntityType.TELogicSensor => entityInfo.EntityData.DeserializeBytes<ProtocolTELogicSensor>(),
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
            => $"ID: [{Id}] - " +
                $"领主: {(string.IsNullOrWhiteSpace(Owner) ? "未知" : Owner)}" +
                $" | 创建: {GetTime:g}" +
                $" | 修改: {LastAccess:g}" +
                $" | 最后一次生成坐标: {X} - {Y}" +
                $" | 箱子数量: {CellChests.Count}" +
                $" | 牌子数量: {CellSigns.Count}" +
                $" | 物块数量: {TileData.To1D().Count(t => !t.isTheSameAs(StructTile.Empty))}";
        public bool SaveCellData()
        {
            if (!IsVisiable)
                return false;

            short startX = X;
            short startY = Y;

            //保存世界中的entity
            Entities.Clear();
            var rec = new Rectangle(startX, startY, Width, Height);
            TileEntity.ByPosition.Where(t => rec.Contains(t.Key.X, t.Key.Y))
                .ForEach(entity =>
                {
                    var e = Activator.CreateInstance(Constants.tileEntityDict[(TileEntityType)entity.Value.type], new object[] { entity.Value }) as IProtocolTileEntity;
                    e.Position = new(e.Position.X - startX, e.Position.Y - startY); //转换为相对坐标
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
                .ForEach(c =>
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
                .ForEach(s =>
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
        public void Invisiable(bool sendSection = true)
        {
            if (!IsVisiable)
                return;

            SaveCellData(); //先保存数据

            UsingCellPositionIndex.Clear(); //移除占用的位置

            ClearTiles(false); //清除物块, 这里不需要同步section

            //删除地图上的entity
            ClearEntities();

            //移除箱子牌子信息
            DeregisteChestAndSign();

            //重新画线
            Parent.ReDrawLines(sendSection);

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
        public bool ShowCell(TSPlayer plr = null)
        {
            if (IsVisiable)
                return false;
            if (this.FindUseableArea() is { } pos)
            {
                LastPositionIndex = pos.First().Index;
                UsingCellPositionIndex.AddRange(pos.Select(p => p.Index));

                UpdateSingle(c => c.LastPositionIndex);
                UpdateSingle(c => c.UsingCellPositionIndex);

                //还原物块
                RestoreCellTileData(false);

                //还原entity
                RestoreEntities();

                //还原箱子牌子
                RegisteChestAndSign();

                TileHelper.ResetSection(X, Y, Width, Height); //发送更改后的区块数据

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

        public void RestoreEntities()
        {
            List<Packet> packetData = new(); //合并数据包 减少发包次数
            Entities.ForEach(entity =>
            {
                var entityX = X + entity.Position.X;
                var entityY = Y + entity.Position.Y;
                TileEntity.PlaceEntityNet((int)entity.EntityType, entityX, entityY);
                if (TileEntity.ByPosition.TryGetValue(new(entityX, entityY), out var placedEntity))
                {
                    //更新原有信息
                    entity.Position = new(placedEntity.Position.X - X, placedEntity.Position.Y - Y);
                    entity.ID = placedEntity.ID;
                    packetData.Add(new TileEntitySharing()
                    {
                        ID = placedEntity.ID,
                        IsNew = true,
                        Entity = entity
                    });
                }
            });
            BInfo.OnlinePlayers.ForEach(p => p.SendPackets(packetData));
        }
        #endregion

        #endregion
    }
}