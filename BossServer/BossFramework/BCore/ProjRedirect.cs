using BossFramework.BAttributes;
using BossFramework.BModels;
using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading.Tasks;
using TrProtocol.Packets;

namespace BossFramework.BCore
{
    /// <summary>
    /// 弹幕重定向
    /// </summary>
    public static class ProjRedirect
    {
        public static ProjRedirectContext DefaultProjContext { get; private set; }

        [AutoInit(order: 200)]
        public static void InitProjRedirect()
        {
            BLog.DEBUG("初始化弹幕重定向");
            DefaultProjContext = new(null);

            BRegionSystem.EnterBRegion += OnEnterRegion;
            BRegionSystem.LeaveBRegion += OnLeaveRegion;

            Task.Run(RedirectLoop);
        }
        public static ConcurrentQueue<(BPlayer plr, SyncProjectile proj)> SyncProjsQueue { get; } = new();
        public static void RedirectLoop()
        {
            while (!Terraria.Netplay.Disconnect)
            {
                try
                {
                    if (SyncProjsQueue.TryDequeue(out var projInfo) && projInfo.plr?.IsRealPlayer == true)
                    {
                        var proj = projInfo.proj;
                        var plr = projInfo.plr;
                        (plr.CurrentRegion ?? BRegion.Default).ProjContext.CreateOrSyncProj(plr, proj);
                    }
                }
                catch (Exception ex)
                {
                    BLog.Error(ex);
                }
                Task.Delay(1).Wait();
            }
        }
        public static void OnProjDestory(BPlayer plr, KillProjectile killProj)
        {
            (plr.CurrentRegion ?? BRegion.Default).ProjContext.DestroyProj(killProj);
        }
        public static void OnEnterRegion(BRegionSystem.BRegionEventArgs args)
        {
            Task.Run(() => args.Region.ProjContext.Projs
                .Where(p => p != null)
                .ForEach(p => args.Player.SendPacket(p))); //同步当前区域弹幕
        }
        public static void OnLeaveRegion(BRegionSystem.BRegionEventArgs args)
        {
            Task.Run(() => args.Region.ProjContext.Projs.Where(p => p != null)
                .ForEach(p => args.Region.ProjContext.DestroyProj(p, false))); //移除所有之前区域的弹幕
        }
    }
}
