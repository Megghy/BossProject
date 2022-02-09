using BossFramework.BAttributes;
using BossFramework.BInterfaces;
using BossFramework.BModels;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Terraria.ID;
using TerrariaApi.Server;
using TrProtocol.Packets;

namespace BossFramework.BCore
{
    public static class BWeaponSystem
    {
        public const short FillItem = 3853;
        public static string WeaponScriptPath => Path.Combine(ScriptManager.ScriptRootPath, "Weapons");
        [AutoPostInit]
        private static void InitWeapon()
        {
            BLog.DEBUG("初始化自定义武器");

            if (!Directory.Exists(WeaponScriptPath))
                Directory.CreateDirectory(WeaponScriptPath);

            LoadWeapon();

            ServerApi.Hooks.GameUpdate.Register(BPlugin.Instance, OnGameUpdate);
        }
        [Reloadable]
        private static void LoadWeapon()
        {
            BWeapons = ScriptManager.LoadScripts<BaseBWeapon>(WeaponScriptPath);
            BLog.Success($"成功加载 {BWeapons.Length} 个自定义武器");
        }

        public static BaseBWeapon[] BWeapons { get; private set; }
        public static void OnGameUpdate(System.EventArgs args)
        {
            BInfo.OnlinePlayers.Where(p => p.IsCustomWeaponMode)
                .ForEach(p =>
                {
                    if (p.TrPlayer.controlUseItem)
                        p.Weapons.FirstOrDefault(w => w.Equals(p.TrPlayer.selectedItem))
                        ?.OnUseItem(p, BInfo.GameTick);
                });
        }
        public static void CheckIncomeItem(BPlayer plr, SyncEquipment item)
        {
            if (item.ItemType == 0 || item.ItemSlot == 59) //移除物品或者拿手上则忽略
                return;
            var localItem = plr.TrPlayer?.inventory[item.ItemSlot];
            if (plr.Weapons?.Where(w => w.ItemID == item?.ItemType && w.Prefix == item?.Prefix).FirstOrDefault() is { } bweapon)
            {
                var targetItem = plr.TrPlayer?.inventory[item.ItemSlot];
                var itemInHand = plr.TsPlayer.ItemInHand;
                if ((targetItem is null || targetItem.type == 0)
                    && ((itemInHand.type == bweapon.ItemID && itemInHand.prefix == bweapon.Prefix)
                    || (itemInHand.type != bweapon.ItemID || itemInHand.prefix != bweapon.Prefix))) //如果目标为空物品并且手上没拿东西或者拿的东西不一样
                    plr.SpawnBWeapon(bweapon, item.ItemSlot);
            }
        }

        #region 物品操作
        private static void FillInventory(this BPlayer plr, int lastSlot = 60)
        {
            var slotPacket = new SyncEquipment()
            {
                ItemType = FillItem,
                PlayerSlot = plr.Index,
                Prefix = 80,
                Stack = 1,

            };
            plr.TsPlayer?.TPlayer.inventory.ForEach((item, i) =>
            {
                if (i < lastSlot && (item is null || item?.type == 0))
                {
                    slotPacket.ItemSlot = (short)i;
                    plr.SendPacket(slotPacket);
                }
            });
        }
        private static void RemoveFillItems(this BPlayer plr)
        {
            var slotPacket = new SyncEquipment()
            {
                ItemType = 0,
                PlayerSlot = plr.Index,
                Prefix = 0,
                Stack = 0
            };
            plr.TsPlayer?.TPlayer.inventory.ForEach((item, i) =>
            {
                if (item is { type: FillItem, prefix: 80 })
                {
                    slotPacket.ItemSlot = (short)i;
                    plr.SendPacket(slotPacket);
                }
            });
        }
        private static void SpawnBWeapon(this BPlayer plr, BaseBWeapon weapon, int slot)
        {
            var item = TShockAPI.TShock.Utils.GetItemById(weapon.ItemID);
            item.prefix = (byte)weapon.Prefix;
            plr.RemoveItem(slot); //先移除旧的物品
            plr.TsPlayer.TPlayer.inventory[slot] = item; //将玩家背标目标位置更改为指定物品

            var itemID = Terraria.Item.NewItem(plr.TsPlayer.TPlayer.position, default, weapon.ItemID, weapon.Stack ?? 1, true, weapon.Prefix);
            plr.SendPacket(new SyncItem()
            {
                ItemSlot = (short)itemID,
                Owner = plr.Index,
                Prefix = (byte)weapon.Prefix,
                ItemType = (short)weapon.ItemID,
                Position = new(plr.TsPlayer.TPlayer.position.X, plr.TsPlayer.TPlayer.position.Y),
                Stack = (short)(weapon.Stack ?? 1),
                Velocity = default
            }); //生成普通物品

            var packet = weapon.TweakePacket;
            packet.ItemSlot = (short)itemID;
            plr.SendPacket(packet); //转换为自定义物品
        }
        internal static void ChangeItemToBWeapon(this BPlayer plr, BaseBWeapon weapon)
        {
            if (!plr.IsCustomWeaponMode)
                return;
            var items = new List<int>();
            plr.TsPlayer?.TPlayer?.inventory.ForEach((item, i) =>
            {
                if (item.type == weapon.ItemID && item.prefix == weapon.Prefix)
                {
                    items.Add(i);
                }
            });
            if (!items.Any())
                return;
            plr.RemoveItem(59); //去掉手上的东西
            plr.FillInventory(items.Last() + 1); //填充至指定位置
            items.ForEach(i => plr.SpawnBWeapon(weapon, i));
            Task.Run(plr.RemoveFillItems); //清理占位物品
        }
        private static void ChangeAllItemsToBWeapon(this BPlayer plr)
        {
            plr.Weapons = (from w in BWeapons select (BaseBWeapon)Activator.CreateInstance(w.GetType(), null)).ToArray(); //给玩家生成武器对象
            plr.RemoveItem(59); //去掉手上的东西
            plr.FillInventory(); //先填满没东西的格子
            plr.TsPlayer?.TPlayer?.inventory.ForEach((item, i) =>
            {
                if (item is { type: not 0 } && BWeapons.Where(w => w.ItemID == item.type && w.Prefix == item.prefix).FirstOrDefault() is { } bweapon)
                    plr.SpawnBWeapon(bweapon, i);
            });
            Task.Run(plr.RemoveFillItems); //清理占位物品
        }
        public static void ChangeCustomWeaponMode(this BPlayer plr, bool? enable = null)
        {
            var oldMode = plr.IsCustomWeaponMode;
            plr.IsCustomWeaponMode = enable ?? !plr.IsCustomWeaponMode;
            plr.TsPlayer.SetBuff(BuffID.Webbed, 60, true); //冻结一秒
            if (oldMode != plr.IsCustomWeaponMode)
            {
                if (plr.IsCustomWeaponMode)
                    Task.Run(() => plr.ChangeAllItemsToBWeapon());
            }
        }
        #endregion
    }
}
