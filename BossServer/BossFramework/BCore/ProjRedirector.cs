using System.Collections.Concurrent;
using System.Linq;
using System.Threading.Tasks;
using BossFramework.BAttributes;
using BossFramework.BModels;
using TrProtocol.Packets;
using TShockAPI;

namespace BossFramework.BCore
{
    /// <summary>
    /// 弹幕重定向
    /// </summary>
    public static class ProjRedirector
    {

        public static ProjRedirectContext DefaultProjContext { get; private set; }

        [AutoInit(order: 200)]
        private static void InitProjRedirect()
        {
            BLog.DEBUG("初始化弹幕重定向");
            if (!BConfig.Instance.EnableProjRedirect)
            {
                BLog.Info("根据配置文件 禁用弹幕重定向");
                return;
            }
            DefaultProjContext = new(null);

            BNet.PacketHandlers.DestroyProjectileHandler.Get += OnProjDestory;
            BNet.PacketHandlers.SyncProjectileHandler.Get += OnProjSync;

            BRegionSystem.EnterBRegion += OnEnterRegion;
            BRegionSystem.LeaveBRegion += OnLeaveRegion;

            Task.Run(RedirectLoop);
        }
        public delegate void OnProjCreate(BEventArgs.ProjCreateEventArgs args);
        public static event OnProjCreate ProjCreate;
        public delegate void OnProjDestroy(BEventArgs.ProjDestroyEventArgs args);
        public static event OnProjDestroy ProjDestroy;
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
                        var args = new BEventArgs.ProjCreateEventArgs(proj, plr);
                        ProjCreate?.Invoke(args);
                        if (!args.Handled)
                        {
                            plr.CurrentRegion.ProjContext.CreateOrSyncProj(plr, proj, proj.PlayerSlot == 255);
                        }
                    }
                    else
                        Task.Delay(1).Wait();
                }
                catch (Exception ex)
                {
                    BLog.Error(ex);
                }
            }
        }
        public static void OnProjSync(BEventArgs.PacketHookArgs<SyncProjectile> args)
            => SyncProjsQueue.Enqueue((args.Player, args.Packet));
        public static void OnProjDestory(BEventArgs.PacketHookArgs<KillProjectile> args)
        {
            ProjDestroy?.Invoke(new(args.Packet, args.Player));
            args.Player.CurrentRegion.ProjContext.DestroyProj(args.Packet);
        }
        public static void OnEnterRegion(BEventArgs.BRegionEventArgs args)
        {
            Task.Run(() => BUtils.SendPacketsTo(args.Region.ProjContext.Projs
                .Where(p => p != null)
                .Select(p => p), args.Player)); //同步当前区域弹幕
        }
        public static void OnLeaveRegion(BEventArgs.BRegionEventArgs args)
        {
            Task.Run(() => args.Region.ProjContext.Projs.Where(p => p?.PlayerSlot == args.Player.Index)
                .ForEach(p => args.Region.ProjContext.DestroyProj(p, false))); //移除所有之前区域的弹幕
        }
    }
}
