using BossFramework.BAttributes;
using BossFramework.BModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TerrariaApi.Server;
using TShockAPI;

namespace BossFramework.BModules
{
    public static class OnlineTimeManager
    {
        [AutoInit]
        public static void InitOnlineTime()
        {
            ServerApi.Hooks.ServerLeave.Register(BossPlugin.Instance, OnPlayerLeave);
            ServerApi.Hooks.GameUpdate.Register(BossPlugin.Instance, OnGameUpdate);
            ServerApi.Hooks.WorldSave.Register(BossPlugin.Instance, OnWorldSave);
        }

        public static void OnWorldSave(EventArgs args)
        {
            DB.DBTools.SQL.Update<BPlayer>().SetSource(BInfo.OnlinePlayers).Set(p => p.OnlineTicks).ExecuteAffrows();
            BLog.Info($"已保存玩家在线时长");
        }

        public static void OnGameUpdate(EventArgs args)
        {
            if(BInfo.GameTick % 60 == 0)
            {
                BInfo.OnlinePlayers.ForEach(p =>
                {
                    p.OnlineTicks += 10000000; //1s
                });
            }
        }

        public static void OnPlayerLeave (LeaveEventArgs args)
        {
            if (TShock.Players[args.Who]?.GetBPlayer() is { } plr)
            {
                plr.Update(p => p.OnlineTicks);
            }
        }
    }
}
