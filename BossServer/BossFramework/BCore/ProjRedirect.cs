using BossFramework.BAttributes;
using BossFramework.BModels;
using System.Linq;
using TrProtocol.Packets;
using TShockAPI.Hooks;

namespace BossFramework.BCore
{
    /// <summary>
    /// 弹幕重定向
    /// </summary>
    public static class ProjRedirect
    {
        public static ProjRedirectContext DefaultProjContext { get; private set; }

        [AutoInit]
        public static void InitProjRedirect()
        {
            BLog.DEBUG("初始化弹幕重定向");
            DefaultProjContext = new(null);

            RegionHooks.RegionEntered += OnEnterRegion;
            RegionHooks.RegionCreated += OnRegionCreate;
        }

        public static bool OnProjCreate(BPlayer plr, SyncProjectile proj)
        {
            if (plr?.TsPlayer?.CurrentRegion is { } region)
            {
                BInfo.OnlinePlayers.Where(p => p != plr && p.TsPlayer?.CurrentRegion == region)
                    .ForEach(p => p.SendPacket(proj));
                return true;
            }
            else
                return false;
        }
        public static void OnRegionCreate(RegionHooks.RegionCreatedEventArgs args)
        {

        }
        public static void OnRegionDelete(RegionHooks.RegionCreatedEventArgs args)
        {

        }
        public static void OnEnterRegion(RegionHooks.RegionEnteredEventArgs args)
        {

        }
    }
}
