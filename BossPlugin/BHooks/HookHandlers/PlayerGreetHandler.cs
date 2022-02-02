using BossPlugin.BModels;
using System.Linq;
using TerrariaApi.Server;
using TShockAPI;

namespace BossPlugin.BHooks.HookHandlers
{
    public static class PlayerGreetHandler
    {
        public static void OnGreetPlayer(GreetPlayerEventArgs args)
        {
            var tsPlr = TShock.Players[args.Who];
            if (tsPlr != null && tsPlr.Account is { } account)
            {
                var bPlr = DB.DBTools.GetSingle<BPlayer>(account.ID.ToString());
                bPlr.TsPlayer = tsPlr;
                tsPlr.SetData("BossPlugin.BPlayer", bPlr);
                if (BCore.MiniGameManager.Games.FirstOrDefault() is { } game)
                    BCore.MiniGameManager.CreateGame(game, bPlr).Join(bPlr); //测试
            }
        }
    }
}
