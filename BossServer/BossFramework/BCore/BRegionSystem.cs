using BossFramework.BAttributes;
using BossFramework.BInterfaces;
using BossFramework.BModels;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using TerrariaApi.Server;
using TShockAPI.DB;
using TShockAPI.Hooks;
using static BossFramework.BModels.BEventArgs;

namespace BossFramework.BCore
{
    public static class BRegionSystem
    {
        public static List<BRegion> AllBRegion { get; private set; }
        public static BaseRegionTagProcessor[] RegionTagProcessers { get; private set; }
        public static string RegionTagPath => Path.Combine(ScriptManager.ScriptRootPath, "RegionTags");

        [AutoPostInit]
        private static void InitRegion()
        {
            BLog.DEBUG("初始化区域管理");

            AllBRegion = DB.DBTools.GetAll<BRegion>().Where(r => r.WorldId == Terraria.Main.worldID).ToList();
            AllBRegion.Insert(0, BRegion.Default);

            TShockAPI.TShock.Regions.Regions.ForEach(r =>
            {
                if (!AllBRegion.Exists(bregion => bregion.OriginRegion == r))
                {
                    OnRegionCreate(new(r)); //不存在则新建
                }
            });
            LoadRegionTags();
            BLog.Success($"共加载 {AllBRegion.Count} 个区域");

            RegionHooks.RegionEntered += OnEnterRegion;
            RegionHooks.RegionLeft += OnLeaveRegion;
            RegionHooks.RegionCreated += OnRegionCreate;
            RegionHooks.RegionDeleted += OnRegionDelete;
            ServerApi.Hooks.GameUpdate.Register(BossPlugin.Instance, OnGameUpdate);
        }
        [Reloadable]
        private static void LoadRegionTags()
        {
            if (!Directory.Exists(RegionTagPath))
                Directory.CreateDirectory(RegionTagPath);
            RegionTagProcessers?.ForEach(t => t.Dispose());
            RegionTagProcessers = ScriptManager.LoadScripts<BaseRegionTagProcessor>(RegionTagPath);
            BLog.Success($"成功加载 {RegionTagProcessers.Length} 个区域标签处理器");
            RegionTagProcessers = new BaseRegionTagProcessor[] { new testtag() };
        }

        public delegate void OnEnterBRegion(BRegionEventArgs args);
        public static event OnEnterBRegion EnterBRegion;
        public delegate void OnLeaveBRegion(BRegionEventArgs args);
        public static event OnLeaveBRegion LeaveBRegion;
        public static BRegion FindBRegionByName(string name)
            => AllBRegion.FirstOrDefault(r => r.Name == name && r.WorldId == Terraria.Main.worldID);
        public static BRegion FindBRegionForRegion(Region region)
        {
            if (region is null)
                return null;
            return AllBRegion.FirstOrDefault(r => r.Name == region.Name && r.WorldId.ToString() == region.WorldID);
        }
        /// <summary>
        /// 返回指定领地中的所有玩家, 提供参数为null时返回所有未在领地中的玩家
        /// </summary>
        /// <param name="region"></param>
        /// <returns></returns>
        public static BPlayer[] GetAllPlayerInRegion(this BRegion region)
        {
            return BInfo.OnlinePlayers.Where(p => p.CurrentRegion == region).ToArray();
        }

        public static void OnRegionCreate(RegionHooks.RegionCreatedEventArgs args)
        {
            var bregion = new BRegion(args.Region);
            AllBRegion.Add(bregion);
            DB.DBTools.Insert(bregion);
            BLog.DEBUG($"区域事件: [创建] - {bregion.Id}");
        }
        public static void OnRegionDelete(RegionHooks.RegionDeletedEventArgs args)
        {
            if (FindBRegionForRegion(args.Region) is { } bregion)
            {
                AllBRegion.ForEach(r =>
                {
                    if (r.Parent == bregion)
                        r.SetParent(null);
                    if (r.ChildRegion.Contains(r))
                        r.RemoveChild(r);
                });
                DB.DBTools.Delete(bregion);
                BLog.DEBUG($"区域事件: [移除] - {bregion.Id}");
            }
        }
        public static void OnGameUpdate(EventArgs args)
        {
            AllBRegion.ForEach(r => RegionTagProcessers.ForEach(t => t.GameUpdate(r, BInfo.GameTick)));
        }
        public static void OnEnterRegion(RegionHooks.RegionEnteredEventArgs args)
        {
            if (FindBRegionForRegion(args.Region) is { } bregion)
            {
                CallBRegionEvent(bregion, args.Player.GetBPlayer(), true);
                RegionTagProcessers.ForEach(t => t.EnterRegion(bregion, args.Player.GetBPlayer()));
            }
        }
        public static void OnLeaveRegion(RegionHooks.RegionLeftEventArgs args)
        {
            if (FindBRegionForRegion(args.Region) is { } bregion)
            {
                CallBRegionEvent(bregion, args.Player.GetBPlayer(), false);
                RegionTagProcessers.ForEach(t => t.LeaveRegion(bregion, args.Player.GetBPlayer()));
            }
        }
        private static void CallBRegionEvent(BRegion region, BPlayer plr, bool isEnter)
        {
            if (region.Parent is { } parent)
                CallBRegionEvent(parent, plr, isEnter);
            else
            {
                BLog.DEBUG($"区域事件: [{(isEnter ? "进入" : "离开")}] - {region.Id} : {plr}");
                var args = new BRegionEventArgs(region, plr);
                if (isEnter)
                {
                    EnterBRegion?.Invoke(args);
                    plr.CurrentRegion = region;
                }
                else
                {
                    LeaveBRegion?.Invoke(args);
                    if (plr.CurrentRegion == region)
                        plr.CurrentRegion = BRegion.Default;
                }
            }
        }
    }
}
