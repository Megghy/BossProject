using BossFramework.BAttributes;
using BossFramework.BModels;
using BossFramework.DB;
using System;
using System.Collections.Generic;
using System.Linq;
using TrProtocol;
using TrProtocol.Models;
using TrProtocol.Packets;

namespace BossFramework.BCore
{
    public static class ChestRedirector
    {
        public static List<BChest> Chests { get; set; }
        private static List<BChest> _overrideChest { get; set; } = new();

        [AutoPostInit]
        private static void InitChest()
        {
            BLog.DEBUG("初始化箱子重定向");

            Chests = DBTools.GetAll<BChest>().Where(r => r.WorldId == Terraria.Main.worldID).ToList();

            Terraria.Main.chest.Where(s => s != null).BForEach(chest =>
            {
                if (!Chests.Exists(c => c.X == chest.x && c.Y == chest.y))
                {
                    CreateChest(chest.x, chest.y, chest.name, chest.item.Select(i => ItemData.Get(i)).ToArray(), null); //不存在则新建
                }
            });

            BLog.Success($"共加载 {Chests.Count} 个箱子");
        }

        #region 事件
        public static void OnGetName(BPlayer plr, ChestName packet)
        {
            if (FindChestFromPos(packet.Position.X, packet.Position.Y) is { } c)
            {
                packet.Name = c.Name;
                packet.NameLength = (byte)(c.Name?.Length ?? 0);
                plr.SendPacket(packet);
            }
        }
        public static void OnChestOpen(BPlayer plr, RequestChestOpen packet)
        {
            if (FindChestFromPos(packet.Position.X, packet.Position.Y) is { } c)
                c.PlayerOpenChest(plr);
            else //不确定要不要生成, 要是有人一直代码发包就能一直创建了
                CreateChest(packet.Position.X, packet.Position.Y, "", new ItemData[40], plr)
                        .PlayerOpenChest(plr);
            if (Terraria.WorldGen.IsChestRigged(packet.Position.X, packet.Position.Y))
            {
                Terraria.Wiring.SetCurrentUser(plr.Index);
                Terraria.Wiring.HitSwitch(packet.Position.X, packet.Position.Y);
                Terraria.Wiring.SetCurrentUser();
                BUtils.SendPacketToAll(new HitSwitch()
                {
                    Position = packet.Position
                }, plr);
            }
        }
        public static void OnUpdateChestItem(BPlayer plr, SyncChestItem packet)
        {
            if (packet.ChestSlot is > -1 and < 8000 && plr.WatchingChest.HasValue)
            {
                var chest = plr.WatchingChest.Value.chest;

                chest.Items[packet.ChestItemSlot] ??= new();
                chest.Items[packet.ChestItemSlot].ItemID = packet.ItemType;
                chest.Items[packet.ChestItemSlot].Prefix = packet.Prefix;
                chest.Items[packet.ChestItemSlot].Stack = packet.Stack;
                plr.WatchingChest.Value.chest.Items = chest.Items;
                plr.WatchingChest.Value.chest.LastUpdateUser = (int)plr.Id;

                //同步给同样在看这个箱子的玩家
                BInfo.OnlinePlayers.Where(p => p.WatchingChest?.chest == chest && p != plr)
                    .BForEach(p =>
                    {
                        packet.ChestSlot = p.WatchingChest.Value.slot;
                        p.SendPacket(packet);
                    });
            }
        }
        public static void OnSyncActiveChest(BPlayer plr, SyncPlayerChest packet)
        {
            Console.WriteLine($"{packet.ChestSlot}, {plr.WatchingChest?.slot} {plr.WatchingChest?.chest}");
            if (packet.ChestSlot == -1 && plr.WatchingChest.HasValue) //-1时为退出箱子
            {
                var chest = plr.WatchingChest?.chest;
                plr.WatchingChest = null;
                if (chest.Name != packet.ChestName && !string.IsNullOrEmpty(packet.ChestName))
                {
                    chest.Name = packet.ChestName;
                    plr.SendSuccessMsg($"已修改箱子名称为 {chest.Name}");
                }
                DBTools.SQL.Update<BChest>(chest)
                    .Set(c => c.LastUpdateUser, chest.LastUpdateUser)
                    .Set(c => c.Items, chest.Items)
                    .Set(c => c.Name, chest.Name)
                    .Set(c => c.UpdateTime, DateTime.Now)
                    .ExecuteAffrows();

                BLog.DEBUG($"{plr} 关闭箱子 {chest}");
            }
        }
        public static void OnPlaceOrDestroyChest(BPlayer plr, ChestUpdates packet)
        {
            if (packet.Operation is 1 or 3 or 5 && FindChestFromPos(packet.Position.X, packet.Position.Y) is { } chest) //破坏箱子
            {
                RemoveChest(chest);
                BLog.DEBUG($"箱子数据移除, 位于 [{packet.Position.X} - {packet.Position.Y}], 来自 {plr}");
            }
        }
        #endregion

        public static BChest FindChestFromPos(int tileX, int tileY)
            => _overrideChest.LastOrDefault(c => c.Contains(tileX, tileY)) ?? Chests.LastOrDefault(s => s.Contains(tileX, tileY));
        public static BChest CreateChest(int tileX, int tileY, string name, ItemData[] items = null, BPlayer plr = null)
        {
            var i = new ItemData[40];
            items?.CopyTo(i, 0);
            var chest = new BChest()
            {
                X = (short)tileX,
                Y = (short)tileY,
                Items = i,
                Owner = (int)(plr?.Id ?? -1),
                LastUpdateUser = (int)(plr?.Id ?? -1),
                WorldId = Terraria.Main.worldID,
                Name = name
            };
            DBTools.Insert(chest);
            Chests.Add(chest);
            BLog.DEBUG($"创建箱子数据于 {chest.X} - {chest.Y} {(plr is null ? "" : $"来自 {plr}")}");
            return chest;
        }

        #region 箱子操作
        public static void PlayerOpenChest(this BChest chest, BPlayer target)
        {
            short slot = 7999;
            target.WatchingChest = new(slot, chest);

            target.SendPackets(GetChestItemPakcets(chest, slot)); //同步物品

            target.SendPacket(GetSyncChestInfoPacket(chest, target, slot)); //同步箱子信息

            target.SendPacket(new ChestName()
            {
                ChestSlot = slot,
                Name = chest.Name,
                NameLength = (byte)(chest.Name?.Length ?? 0),
                Position = new(chest.X, chest.Y)
            });

            BLog.DEBUG($"{target} 打开箱子 {chest}");
        }
        public static short GetNextChestIndex(this BPlayer plr)
        {
            if (plr.LastSyncChestIndex > 7998 || plr.LastSyncChestIndex < 0)
                plr.LastSyncChestIndex = 0;
            else
                plr.LastSyncChestIndex++;
            return plr.LastSyncChestIndex;
        }
        public static SyncPlayerChest GetSyncChestInfoPacket(BChest chest, BPlayer plr, short slot = -1)
        {
            return new()
            {
                ChestSlot = slot == -1 ? plr.GetNextChestIndex() : slot,
                ChestName = chest.Name,
                ChestNameLength = (byte)(chest.Name?.Length ?? 0),
                Position = new(chest.X, chest.Y)
            };
        }
        public static void SyncChestItem(this BChest chest, short chestSlot)
        {
            BUtils.SendPacketsToAll(GetChestItemPakcets(chest, chestSlot));
        }
        private static List<Packet> GetChestItemPakcets(this BChest chest, short chestSlot)
        {
            List<Packet> list = new();
            40.ForEach(i =>
            {
                chest.Items[i] ??= new();
                var c = chest.Items[i];
                list.Add(new SyncChestItem()
                {
                    ChestSlot = chestSlot,
                    ChestItemSlot = (byte)i,
                    ItemType = c.ItemID,
                    Prefix = c.Prefix,
                    Stack = c.Stack,
                });
            });
            return list;
        }
        public static bool RemoveChest(int tileX, int tileY)
        {
            if (FindChestFromPos(tileX, tileY) is { } chest)
            {
                RemoveChest(chest);
                return true;
            }
            return false;
        }
        public static void RemoveChest(BChest chest)
        {
            BInfo.OnlinePlayers.Where(p => p.WatchingChest?.chest == chest)
                    .BForEach(p => p.WatchingChest = null);
            if (!DeregisterOverrideChest(chest))
                if (Chests.Remove(chest))
                    DBTools.Delete(chest);
        }
        #endregion

        public static void RegisterOverrideChest(short x, short y, ItemData[] items)
        {
            var newItems = new ItemData[40];
            if (items is { Length: > 0 })
                40.ForEach(i =>
                {
                    if (items.Length > i)
                        newItems[i] = items[i] ?? new();
                });
            RegisterOverrideChest(new()
            {
                Items = newItems,
                X = x,
                Y = y
            });
        }
        public static void RegisterOverrideChest(BChest chest)
            => _overrideChest.Add(chest);
        public static bool DeregisterOverrideChest(int tileX, int tileY)
            => _overrideChest.RemoveAll(s => s.Contains(tileX, tileY)) != 0;
        public static bool DeregisterOverrideChest(BChest chest)
            => _overrideChest.Remove(chest);
        /// <summary>
        /// 包含注册的箱子在内的所有箱子
        /// </summary>
        /// <returns></returns>
        public static BChest[] AllChest()
        {
            var result = new List<BChest>();
            result.AddRange(Chests);
            result.AddRange(_overrideChest);
            return result.ToArray();
        }
    }
}
