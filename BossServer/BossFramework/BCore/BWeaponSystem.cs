using BossFramework.BAttributes;
using BossFramework.BInterfaces;
using BossFramework.BModels;
using System;
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

            ProjRedirect.ProjCreate += OnProjCreate;
            ProjRedirect.ProjDestroy += OnProjDestroy;
        }
        [Reloadable]
        private static void LoadWeapon()
        {
            BWeapons = ScriptManager.LoadScripts<BaseBWeapon>(WeaponScriptPath);
            BLog.Success($"成功加载 {BWeapons.Length} 个自定义武器");
        }

        public static BaseBWeapon[] BWeapons { get; private set; }

        #region 事件
        public static void OnGameUpdate(EventArgs args)
        {
            BInfo.OnlinePlayers.Where(p => p.IsCustomWeaponMode)
                .ForEach(p =>
                {
                    if (p.TrPlayer.controlUseItem)
                        p.Weapons.FirstOrDefault(w => p.ItemInHand.NetId == 0 ? w.Equals(p.TsPlayer.SelectedItem) : w.Equals(p.ItemInHand))
                        ?.OnUseItem(p, BInfo.GameTick);
                });
        }
        public static bool CheckIncomeItem(BPlayer plr, SyncEquipment item)
        {
            if (plr.IsChangingWeapon)
                return true;
            if (item.ItemSlot > 50 || item.ItemType == 0) //空格子或者拿手上则忽略
                return false;
            if (plr.Weapons?.Where(w => w.Equals(item)).FirstOrDefault() is { } bweapon)
            {
                var targetItem = plr.TsPlayer.TPlayer.inventory[item.ItemSlot];
                if ((targetItem is null || targetItem?.type == 0)
                    && (plr.ItemInHand.NetId == 0 || !bweapon.Equals(plr.ItemInHand))) //如果目标为空物品并且手上没拿东西或者拿的东西不一样
                {
                    plr.TsPlayer.TPlayer.inventory[item.ItemSlot] ??= new();
                    plr.TsPlayer.TPlayer.inventory[item.ItemSlot].SetDefaults(item.ItemType);
                    plr.TsPlayer.TPlayer.inventory[item.ItemSlot].stack = item.Stack;
                    plr.TsPlayer.TPlayer.inventory[item.ItemSlot].prefix = item.Prefix;
                    plr.ChangeSingleItemToBWeapon(bweapon, item.ItemSlot);
                    BLog.DEBUG($"拾取 BWeapon: [{bweapon.Name}]");
                    return true;
                }
            }
            return false;
        }
        public static void OnPlayerHurt(BPlayer plr, PlayerHurtV2 packet)
        {
            if (plr.IsCustomWeaponMode)
            {
                var targetPlayer = TShock.Players[packet.OtherPlayerSlot]?.GetBPlayer();
                var deathReason = packet.Reason;
                if (plr.RelesedProjs.Where(p => p.proj.ProjSlot == deathReason._sourceProjectileIndex).FirstOrDefault() is { } projInfo)
                {
                    projInfo.fromWeapon.OnProjHit(plr, targetPlayer, projInfo.proj, packet.Damage, packet.HitDirection, (byte)packet.CoolDown);
                }
                else if (plr.Weapons.Where(w => w.ItemID == deathReason._sourceItemType && w.Prefix == deathReason._sourceItemPrefix).FirstOrDefault() is { } weapon)
                {
                    weapon.OnHit(plr, targetPlayer, packet.Damage, packet.HitDirection, (byte)packet.CoolDown);
                }
            }
        }
        public static void OnProjCreate(BEventArgs.ProjCreateEventArgs args)
        {
            if (!args.Player.IsCustomWeaponMode)
                return;
            if (args.Player.Weapons.Where(w => w.Equals(args.Player.TsPlayer.SelectedItem)).FirstOrDefault() is { } weapon)
            {
                var selectItem = args.Player.TsPlayer.SelectedItem;
                if (weapon.OnShootProj(args.Player, args.Proj, new(args.Proj.Velocity.X, args.Proj.Velocity.Y), (weapon.ShootProj ?? selectItem.shoot) == args.Proj.ProjType)) //如果返回true则关闭客户端对应弹幕
                {
                    args.Handled = true;
                    args.Proj.ProjType = 0;
                    args.Player.SendPacket(args.Proj);
                }
            }
        }
        public static void OnProjDestroy(BEventArgs.ProjDestroyEventArgs args)
        {
            if (args.Player.RelesedProjs.Where(p => p.proj.ProjSlot == args.KillProj.ProjSlot).FirstOrDefault() is { } projInfo)
            {
                args.Player.RelesedProjs.Remove(projInfo);
                projInfo.fromWeapon.OnProjDestroy(args.Player, args.KillProj);
            }
        }
        #endregion

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
                if (i < 50 && (item is null || item?.type == 0))
                {
                    plr.TsPlayer.TPlayer.inventory[i] ??= new();
                    plr.TsPlayer.TPlayer.inventory[i].SetDefaults(FillItem);
                    plr.TsPlayer.TPlayer.inventory[i].prefix = 80;
                    slotPacket.ItemSlot = (short)i;
                    plr.SendPacket(slotPacket);
                }
            });
        }
        private static void RemoveFillItems(this BPlayer plr)
        {
            plr.TsPlayer?.TPlayer.inventory.ForEach((item, i) =>
            {
                if (item is { type: FillItem, prefix: 80 })
                    plr.RemoveItem(i);
            });
        }
        private static void SpawnBWeapon(this BPlayer plr, BaseBWeapon weapon, int slot)
        {
            var item = TShock.Utils.GetItemById(weapon.ItemID);
            item.prefix = (byte)weapon.Prefix;
            item.stack = weapon.Stack;
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
        private static void ChangeSingleItemToBWeapon(this BPlayer plr, BaseBWeapon weapon, int slot)
        {
            if (!plr.IsCustomWeaponMode || plr.IsChangingWeapon)
                return;
            plr.IsChangingWeapon = true;

            plr.FillInventory(); //先填满没东西的格子
            plr.SpawnBWeapon(weapon, slot);
            plr.RemoveFillItems(); //清理占位物品

            Task.Run(() =>
            {
                Task.Delay(250).Wait();
                plr.IsChangingWeapon = false;
            });
        }
        private static void ChangeItemToBWeapon(this BPlayer plr, BaseBWeapon weapon = null)
        {
            if (!plr.IsCustomWeaponMode || plr.IsChangingWeapon)
                return;
            plr.IsChangingWeapon = true;

            plr.FillInventory(); //先填满没东西的格子
            for (int i = 49; i >= 0; i--)
            {
                var item = plr.TsPlayer?.TPlayer?.inventory[i];
                if (weapon?.Equals(item) == true)
                    plr.SpawnBWeapon(weapon, i);
                else if (BWeapons.Where(w => w.Equals(item)).FirstOrDefault() is { } bweapon)
                    plr.SpawnBWeapon(bweapon, i);
            }
            plr.RemoveFillItems(); //清理占位物品
            Task.Run(() =>
            {
                Task.Delay(333).Wait();
                plr.IsChangingWeapon = false;
            });
        }
        private static void BackToNormalItem(this BPlayer plr)
        {
            plr.TrPlayer.inventory.ForEach((item, i) =>
            {
                if (BWeapons.Where(w => w.Equals(item)).Any())
                    plr.SendPacket(new SyncEquipment()
                    {
                        ItemSlot = (short)i,
                        ItemType = (short)item.type,
                        PlayerSlot = plr.Index,
                        Prefix = item.prefix,
                        Stack = (short)item.stack
                    });
            });
        }
        public static void ChangeCustomWeaponMode(this BPlayer plr, bool? enable = null)
        {
            var oldMode = plr.IsCustomWeaponMode;
            plr.IsCustomWeaponMode = enable ?? !plr.IsCustomWeaponMode;
            plr.TsPlayer.SetBuff(BuffID.Webbed, 60, true); //冻结
            plr.TsPlayer.SetBuff(BuffID.Stoned, 60, true); //石化
            if (oldMode != plr.IsCustomWeaponMode && !plr.IsChangingWeapon)
            {
                plr.SendPacket(BUtils.GetCurrentWorldData(true));
                if (plr.IsCustomWeaponMode)
                {
                    plr.Weapons = (from w in BWeapons select (BaseBWeapon)Activator.CreateInstance(w.GetType(), null)).ToArray(); //给玩家生成武器对象
                    plr.ChangeItemToBWeapon();
                }
                else
                {
                    plr.Weapons = null;
                    plr.BackToNormalItem();
                }
            }
        }
        #endregion
    }
}
