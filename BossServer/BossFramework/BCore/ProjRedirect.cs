using BossFramework.BAttributes;
using BossFramework.BModels;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TerrariaApi.Server;
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

        [AutoInit(order: 200)]
        public static void InitProjRedirect()
        {
            BLog.DEBUG("初始化弹幕重定向");
            DefaultProjContext = new(null);

            BRegionSystem.EnterBRegion += OnEnterRegion;

            Task.Run(RedirectLoop);
        }
        public static ConcurrentQueue<(BPlayer plr, SyncProjectile proj)> CreateProjsQueue { get; } = new();
        public static async void RedirectLoop()
        {
            while (!Terraria.Netplay.Disconnect)
            {
                try
                {
                    if(CreateProjsQueue.TryDequeue(out var projInfo) && projInfo.plr?.IsRealPlayer == true)
                    {
                        var proj = projInfo.proj;
                        var plr = projInfo.plr;
                        if(plr.CurrentRegion is { } region)
                        {
                            region.ProjContext.
                        }
                    }
                }
                catch (Exception ex)
                {
                    BLog.Error(ex);
                }
                Task.Delay(1).Wait();
            }
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
        public static void OnEnterRegion(BRegionSystem.BRegionEventArgs args)
        {
             
        }
    }
}
