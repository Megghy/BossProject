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
using TShockAPI;

namespace BossFramework.BCore
{
    public static class BWeaponSystem
    {
        public const short FillItem = 3853;
        public static string WeaponScriptPath => Path.Combine(ScriptManager.ScriptRootPath, "Weapons");
        [AutoInit]
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
                        p.Weapons.FirstOrDefault(w => p.ItemInHand.NetId == 0 ? w.Equals(p.TsPlayer.SelectedItem) : w.Equals(p.ItemInHand))
                        ?.OnUseItem(p, BInfo.GameTick);
                });
        }
        public static void CheckIncomeItem(BPlayer plr, SyncEquipment item)
        {
            BLog.DEBUG($"type: {item.ItemType}, prefix: {item.Prefix}, slot: {item.ItemSlot}");
            if (item.ItemSlot > 60 || item.ItemType == 0 || item.ItemSlot == 58 || plr.IsChangingWeapon) //空格子或者拿手上则忽略
                return;
            if (plr.Weapons?.Where(w => w.ItemID == item?.ItemType && w.Prefix == item?.Prefix).FirstOrDefault() is { } bweapon)
            {
                var targetItem = plr.TrPlayer?.inventory[item.ItemSlot];
                if ((targetItem is null || targetItem?.type == 0)
                    && (plr.ItemInHand.NetId == 0 || !bweapon.Equals(plr.ItemInHand))) //如果目标为空物品并且手上没拿东西或者拿的东西不一样
                {
                    plr.ChangeItemToBWeapon(bweapon);
                    BLog.DEBUG($"拾取 BWeapon: [{bweapon.Name}]");
                }
            }
        }
        public static void OnPlayerHurt(BPlayer plr, PlayerHurtV2 packet)
        {
            if (plr.IsCustomWeaponMode)
            {
                var targetPlayer = TShock.Players[packet.OtherPlayerSlot]?.GetBPlayer();
                var deathReason = packet.Reason;
                if (plr.ProjContext.Projs.Where(p => p is BWeaponRelesedProj tempProj && tempProj.ProjSlot == deathReason._sourceProjectileIndex).FirstOrDefault() is BWeaponRelesedProj projInfo)
                {
                    projInfo.FromWeapon.OnProjHit(plr, targetPlayer, projInfo, packet.Damage, packet.HitDirection, (byte)packet.CoolDown);
                }
                else if (plr.Weapons.Where(w => w.ItemID == deathReason._sourceItemType && w.Prefix == deathReason._sourceItemPrefix).FirstOrDefault() is { } weapon)
                {
                    weapon.OnHit(plr, targetPlayer, packet.Damage, packet.HitDirection, (byte)packet.CoolDown);
                }
            }
        }

        #region 物品操作
        private static void FillInventory(this BPlayer plr)
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
                if (item is null || item?.type == 0)
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
            var item = TShock.Utils.GetItemById(weapon.ItemID);
            item.prefix = (byte)weapon.Prefix;
            plr.RemoveItem(slot); //先移除旧的物品
            plr.TsPlayer.TPlayer.inventory[slot] = item; //将玩家背标目标位置更改为指定物品

            var itemID = Terraria.Item.NewItem(plr.TsPlayer.TPlayer.position, default, weapon.ItemID, weapon.Stack, true, weapon.Prefix);
            plr.SendPacket(new SyncItem()
            {
                ItemSlot = (short)itemID,
                Owner = 255,
                Prefix = (byte)weapon.Prefix,
                ItemType = (short)weapon.ItemID,
                Position = new(plr.TsPlayer.TPlayer.position.X, plr.TsPlayer.TPlayer.position.Y),
                Stack = (short)(weapon.Stack),
                Velocity = default
            }); //生成普通物品

            var packet = weapon.TweakePacket;
            packet.ItemSlot = (short)itemID;
            plr.SendPacket(packet); //转换为自定义物品
            plr.SendPacket(new ItemOwner()
            {
                ItemSlot = (short)itemID,
                OtherPlayerSlot = plr.Index
            });
        }

        internal static void ChangeItemToBWeapon(this BPlayer plr, BaseBWeapon weapon)
        {
            if (!plr.IsCustomWeaponMode || plr.IsChangingWeapon)
                return;
            plr.IsChangingWeapon = true;
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
            plr.FillInventory(); //填充至指定位置
            items.ForEach(i => plr.SpawnBWeapon(weapon, i));
            Task.Run(plr.RemoveFillItems); //清理占位物品
            plr.IsChangingWeapon = false;
        }
        private static void ChangeAllItemsToBWeapon(this BPlayer plr)
        {
            if (!plr.IsCustomWeaponMode || plr.IsChangingWeapon)
                return;
            plr.IsChangingWeapon = true;
            plr.Weapons = (from w in BWeapons select (BaseBWeapon)Activator.CreateInstance(w.GetType(), null)).ToArray(); //给玩家生成武器对象
            plr.RemoveItem(59); //去掉手上的东西
            plr.FillInventory(); //先填满没东西的格子
            plr.TsPlayer?.TPlayer?.inventory.ForEach((item, i) =>
            {
                if (item is { type: not 0 } && BWeapons.Where(w => w.ItemID == item.type && w.Prefix == item.prefix).FirstOrDefault() is { } bweapon)
                    plr.SpawnBWeapon(bweapon, i);
            });
            Task.Run(plr.RemoveFillItems); //清理占位物品
            plr.IsChangingWeapon = false;
        }
        public static void ChangeCustomWeaponMode(this BPlayer plr, bool? enable = null)
        {
            var oldMode = plr.IsCustomWeaponMode;
            plr.IsCustomWeaponMode = enable ?? !plr.IsCustomWeaponMode;
            plr.TsPlayer.SetBuff(BuffID.Webbed, 60, true); //冻结一秒
            if (oldMode != plr.IsCustomWeaponMode && !plr.IsChangingWeapon)
            {
                if (plr.IsCustomWeaponMode)
                    Task.Run(() => plr.ChangeAllItemsToBWeapon());
            }
        }
        #endregion


    }
}
