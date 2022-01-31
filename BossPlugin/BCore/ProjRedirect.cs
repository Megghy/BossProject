using BossPlugin.BModels;
using System.Linq;
using TrProtocol.Packets;

namespace BossPlugin.BCore
{
    /// <summary>
    /// 弹幕重定向
    /// </summary>
    public static class ProjRedirect
    {
        public static bool OnProjCreate(BPlayer plr, SyncProjectile proj)
        {
            if (plr.TsPlayer.CurrentRegion is { } region)
            {
                BInfo.OnlinePlayers.Where(p => p != plr && p.TsPlayer.CurrentRegion == region)
                    .ForEach(p => p.SendPacket(proj));
                return true;
            }
            else
                return false;
        }
    }
}
