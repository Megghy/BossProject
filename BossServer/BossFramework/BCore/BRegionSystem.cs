using BossFramework.BAttributes;
using BossFramework.BModels;
using System.Collections.Generic;
using System.Linq;
using TShockAPI.DB;
using TShockAPI.Hooks;

namespace BossFramework.BCore
{
    public static class BRegionSystem
    {
        public class BRegionEventArgs
        {
            public BRegionEventArgs(BRegion region, BPlayer plr)
            {
                Region = region;
                Player = plr;
            }
            public BRegion Region { get; set; }
            public BPlayer Player { get; set; }
        }
        public static List<BRegion> AllBRegion { get; private set; }

        [AutoPostInit]
        public static void InitRegion()
        {
            BLog.DEBUG("初始化区域管理");

            var s = DB.DBTools.GetAll<BRegion>();
            AllBRegion = DB.DBTools.GetAll<BRegion>().Where(r => r.ID.EndsWith(Terraria.Main.worldID.ToString())).ToList();

            TShockAPI.TShock.Regions.Regions.ForEach(r =>
            {
                if (!AllBRegion.Exists(bregion => bregion.OriginRegion == r))
                {
                    OnRegionCreate(new(r)); //不存在则新建
                }
            });

            RegionHooks.RegionEntered += OnEnterRegion;
            RegionHooks.RegionCreated += OnRegionCreate;
            RegionHooks.RegionDeleted += OnRegionDelete;
        }

        public delegate void OnEnterBRegion(BRegionEventArgs args);
        public static event OnEnterBRegion EnterBRegion;
        public delegate void OnLeaveBRegion(BRegionEventArgs args);
        public static event OnLeaveBRegion LeaveBRegion;

        public static BRegion FindBRegionForRegion(Region region)
        {
            if (region is null)
                return null;
            return AllBRegion.FirstOrDefault(r => r.ID == $"{region.Name}_{region.WorldID}");
        }

        public static void OnRegionCreate(RegionHooks.RegionCreatedEventArgs args)
        {
            var bregion = new BRegion(args.Region);
            AllBRegion.Add(bregion);
            DB.DBTools.Insert(bregion);
            BLog.DEBUG($"区域事件: [创建] - {bregion.ID}");
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
                BLog.DEBUG($"区域事件: [移除] - {bregion.ID}");
            }
        }
        public static void OnEnterRegion(RegionHooks.RegionEnteredEventArgs args)
        {
            if (FindBRegionForRegion(args.Region) is { } bregion)
            {
                CallEnterBRegion(bregion, args.Player.GetBPlayer(), true);
            }
        }
        public static void OnLeaveRegion(RegionHooks.RegionLeftEventArgs args)
        {
            if (FindBRegionForRegion(args.Region) is { } bregion)
            {
                CallEnterBRegion(bregion, args.Player.GetBPlayer(), false);
            }
        }
        private static void CallEnterBRegion(BRegion region, BPlayer plr, bool isEnter)
        {
            if (region.Parent is { } parent)
                CallEnterBRegion(parent, plr, isEnter);
            else
            {
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
                        plr.CurrentRegion = null;
                }
            }
        }
    }
}
