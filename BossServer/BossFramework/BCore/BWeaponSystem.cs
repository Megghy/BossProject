using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using BossFramework.BAttributes;
using BossFramework.BInterfaces;
using BossFramework.BModels;
using Terraria.ID;
using TerrariaApi.Server;
using TrProtocol;
using TrProtocol.Packets;
using TShockAPI;

namespace BossFramework.BCore
{
    public static class BWeaponSystem
    {
        public const short FillItem = 3853;
        public static string WeaponScriptPath => Path.Combine(ScriptManager.ScriptRootPath, "Weapons");
        [AutoInit]
        internal static void InitWeapon()
        {
            BLog.DEBUG("初始化自定义武器");

            if (!Directory.Exists(WeaponScriptPath))
                Directory.CreateDirectory(WeaponScriptPath);

            LoadWeapon();

            ServerApi.Hooks.GameUpdate.Register(BossPlugin.Instance, OnGameUpdate);

            if (BConfig.Instance.EnableProjRedirect)
            {
                ProjRedirector.ProjCreate += OnProjCreate;
                ProjRedirector.ProjDestroy += OnProjDestroy;
            }
            else
            {
                BNet.PacketHandlers.SyncProjectileHandler.Get += args => OnProjCreate(new(args.Packet, args.Player));
                BNet.PacketHandlers.DestroyProjectileHandler.Get += args => OnProjDestroy(new(args.Packet, args.Player));
            }

            BNet.PacketHandlers.PlayerDamageHandler.Get += OnPlayerHurt;
        }
        [Reloadable]
        private static void LoadWeapon()
        {
            BWeapons = ScriptManager.LoadScripts<BaseBWeapon>(WeaponScriptPath);
            BLog.Success($"成功加载 {BWeapons.Length} 个自定义武器");

            BInfo.OnlinePlayers.Where(p => p.IsCustomWeaponMode).ForEach(p =>
            {
                p.Weapons = (from w in BWeapons select (BaseBWeapon)Activator.CreateInstance(w.GetType(), null)).ToArray(); //给玩家生成武器对象
                p.ChangeItemsToBWeapon();
            });
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
        [SimpleTimer(Time = 1)]
        public static void OnSecendUpdate()
        {
            BInfo.OnlinePlayers.Where(p => p?.IsCustomWeaponMode ?? false)
                .ForEach(p =>
                {
                    if (p.RelesedProjs.Where(p => DateTimeOffset.Now.ToUnixTimeSeconds() - p.CreateTime > BInfo.ProjMaxLiveTick).ToArray() is { Length: > 0 } inactiveProjs)
                        inactiveProjs.ForEach(projInfo =>
                        {
                            projInfo.KillProj();
                            p.RelesedProjs.Remove(projInfo); //存活太久则删除
                        });
                });
        }
        public static bool CheckIncomeItem(BPlayer plr, SyncEquipment item)
        {
            if (plr.IsChangingWeapon)
                return true;
            if (item.ItemSlot > 50 || item.ItemType == 0) //空格子或者拿手上则忽略
                return false;
            if (plr.Weapons?.FirstOrDefault(w => w.Equals(item)) is { } bweapon)
            {
                var targetItem = plr.TsPlayer.TPlayer.inventory[item.ItemSlot];
                if ((targetItem is null || targetItem?.type == 0)
                    && (plr.ItemInHand.NetId == 0 || !bweapon.Equals(plr.ItemInHand))) //如果目标为空物品并且手上没拿东西或者拿的东西不一样
                {
                    plr.TsPlayer.TPlayer.inventory[item.ItemSlot] ??= new();
                    plr.TsPlayer.TPlayer.inventory[item.ItemSlot].SetDefaults(item.ItemType);
                    plr.TsPlayer.TPlayer.inventory[item.ItemSlot].stack = item.Stack;
                    plr.TsPlayer.TPlayer.inventory[item.ItemSlot].prefix = item.Prefix;
                    Task.Run(() => plr.ChangeSingleItemToBWeapon(bweapon, item.ItemSlot));
                    return true;
                }
            }
            return false;
        }
        public static void OnPlayerHurt(BEventArgs.PacketHookArgs<PlayerHurtV2> args)
        {
            var plr = args.Player;
            if (plr.IsCustomWeaponMode)
            {
                var hurt = args.Packet;
                var targetPlayer = TShock.Players[hurt.OtherPlayerSlot]?.GetBPlayer();
                var deathReason = hurt.Reason;
                if (plr.RelesedProjs.FirstOrDefault(p => p.Proj.ProjSlot == deathReason._sourceProjectileIndex) is { } projInfo)
                {
                    args.Handled = projInfo.FromWeapon.OnProjHit(plr, targetPlayer, projInfo.Proj, hurt.Damage, hurt.HitDirection, (byte)hurt.CoolDown);
                }
                else if (plr.Weapons.FirstOrDefault(w => w.ItemID == deathReason._sourceItemType && w.Prefix == deathReason._sourceItemPrefix) is { } weapon)
                {
                    args.Handled = weapon.OnHit(plr, targetPlayer, hurt.Damage, hurt.HitDirection, (byte)hurt.CoolDown);
                }
                if (args.Handled) //伤害handle后向造成伤害的玩家同步真实血量
                    plr.SendPacket(new PlayerHealth()
                    {
                        PlayerSlot = targetPlayer.Index,
                        StatLife = (short)targetPlayer.TrPlayer.statLife,
                        StatLifeMax = (short)targetPlayer.TrPlayer.statLifeMax2
                    });
            }
        }
        public static void OnProjCreate(BEventArgs.ProjCreateEventArgs args)
        {
            if (!args.Player.IsCustomWeaponMode || args.Proj.PlayerSlot != args.Player.Index)
                return;
            if (args.Player.Weapons.FirstOrDefault(w => w.Equals(args.Player.TsPlayer.SelectedItem)) is { } weapon)
            {
                args.Handled = true;
                var selectItem = args.Player.TsPlayer.SelectedItem;
                if (weapon.OnShootProj(args.Player, args.Proj, new(args.Proj.Velocity.X, args.Proj.Velocity.Y), (weapon.ShootProj ?? selectItem.shoot) == args.Proj.ProjType)) //如果返回true则关闭客户端对应弹幕
                {
                    var proj = args.Proj;
                    proj.ProjType = 0;
                    args.Proj = proj;
                    var data = args.Proj.SerializePacket();
                    args.Player.CurrentRegion.GetAllPlayerInRegion().ForEach(plr => plr.SendRawData(data));
                }
                else
                    weapon.CreateProj(args.Player, args.Proj);
            }
        }
        public static void OnProjDestroy(BEventArgs.ProjDestroyEventArgs args)
        {
            if (args.Player.RelesedProjs.FirstOrDefault(p => p.Proj.ProjSlot == args.KillProj.ProjSlot) is { } projInfo)
            {
                projInfo.FromWeapon.OnProjDestroy(args.Player, args.KillProj);
                args.Player.RelesedProjs.Remove(projInfo);
            }
        }
        #endregion

        #region 物品操作
        private static void FillInventory(this BPlayer plr)
        {
            List<Packet> packetData = new();
            lock (plr.TrPlayer)
            {
                plr.TrPlayer.inventory.ForEachWithIndex((item, i) =>
                {
                    if (i < 50 && (item is null || item?.type == 0))
                    {
                        plr.TsPlayer.TPlayer.inventory[i] ??= new();
                        plr.TsPlayer.TPlayer.inventory[i].SetDefaults(FillItem);
                        packetData.Add(new SyncEquipment()
                        {
                            ItemType = FillItem,
                            PlayerSlot = plr.Index,
                            Prefix = 0,
                            Stack = 1,
                            ItemSlot = (short)i
                        });
                    }
                });
            }
            plr.SendPackets(packetData);
        }
        private static void RemoveFillItems(this BPlayer plr)
        {
            List<Packet> packetData = new();
            plr.TsPlayer?.TPlayer.inventory.ForEachWithIndex((item, i) =>
            {
                if (item is { type: FillItem })
                {
                    packetData.Add(new SyncEquipment()
                    {
                        ItemType = 0,
                        PlayerSlot = plr.Index,
                        Prefix = 0,
                        Stack = 0,
                        ItemSlot = (short)i
                    });
                    plr.TsPlayer.TPlayer.inventory[i].SetDefaults();
                }
            });
            plr.SendPackets(packetData);
        }
        private static void SpawnBWeapon(this BPlayer plr, BaseBWeapon weapon, int slot)
        {
            plr.TrPlayer.inventory[slot] ??= TShock.Utils.GetItemById(weapon.ItemID);
            plr.TrPlayer.inventory[slot].SetDefaults(weapon.ItemID);
            plr.TrPlayer.inventory[slot].prefix = (byte)weapon.Prefix;
            plr.TrPlayer.inventory[slot].stack = weapon.Stack; //将玩家背标目标位置更改为指定物品

            var packets = new List<Packet>();
            var itemID = 400 - 399;
            packets.Add(new InstancedItem()
            {
                ItemSlot = (short)itemID,
                Owner = 0,
                Prefix = (byte)weapon.Prefix,
                ItemType = (short)weapon.ItemID,
                Position = new(plr.TrPlayer.position.X, plr.TrPlayer.position.Y),
                Stack = (short)weapon.Stack,
                Velocity = default
            }); //生成普通物品

            var packet = weapon.TweakePacket;
            packet.ItemSlot = (short)itemID;
            packets.Add(packet); //转换为自定义物品

            packets.Add(plr.RemoveItemPacket(slot)); //移除旧的物品

            plr.SendPackets(packets);
        }
        private static void ChangeSingleItemToBWeapon(this BPlayer plr, BaseBWeapon weapon, int slot)
        {
            if (!plr.IsCustomWeaponMode || plr.IsChangingWeapon)
                return;
            plr.IsChangingWeapon = true;

            plr.FillInventory(); //先填满没东西的格子
            plr.SpawnBWeapon(weapon, slot);
            plr.RemoveFillItems(); //清理占位物品

            Task.Delay(200).Wait();
            plr.IsChangingWeapon = false;
        }
        public static void ChangeItemsToBWeapon(this BPlayer plr)
        {
            if (!plr.IsCustomWeaponMode || plr.IsChangingWeapon)
                return;
            plr.IsChangingWeapon = true;

            plr.TsPlayer.SetBuff(BuffID.Webbed, 60, true); //冻结
            plr.TsPlayer.SetBuff(BuffID.Stoned, 60, true); //石化
            plr.SendPacket(BUtils.GetCurrentWorldData(true)); //强制开启ssc

            plr.FillInventory(); //先填满没东西的格子
            for (int i = 49; i >= 0; i--)
            {
                var item = plr.TsPlayer?.TPlayer?.inventory[i];
                if (BWeapons.FirstOrDefault(w => w.Equals(item)) is { } bweapon)
                {
                    plr.SpawnBWeapon(bweapon, i);
                    Task.Delay(BConfig.Instance.ChangWeaponDelay).Wait();
                }
            }
            plr.RemoveFillItems(); //清理占位物品
            plr.SendPacket(BUtils.GetCurrentWorldData(Terraria.Main.ServerSideCharacter)); //关闭ssc

            Task.Delay(200).Wait();
            plr.IsChangingWeapon = false;
        }
        public static void BackToNormalItem(this BPlayer plr)
        {
            plr.TrPlayer.inventory.ForEachWithIndex((item, i) =>
            {
                if (BWeapons.Any(w => w.Equals(item)))
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
            if (oldMode != plr.IsCustomWeaponMode && !plr.IsChangingWeapon)
            {
                plr.SendPacket(BUtils.GetCurrentWorldData(true));
                if (plr.IsCustomWeaponMode)
                {
                    plr.Weapons = (from w in BWeapons select (BaseBWeapon)Activator.CreateInstance(w.GetType(), null)).ToArray(); //给玩家生成武器对象
                    Task.Run(plr.ChangeItemsToBWeapon);
                }
                else
                {
                    plr.Weapons = null;
                    plr.RelesedProjs.ForEach(r => r.Proj.Inactive());
                    plr.RelesedProjs.Clear();
                    plr.BackToNormalItem();
                }
            }
        }
        #endregion
    }
}
