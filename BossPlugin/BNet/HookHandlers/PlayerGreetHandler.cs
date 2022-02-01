using BossPlugin.BModels;
using TerrariaApi.Server;
using TShockAPI;

namespace BossPlugin.BNet.HookHandlers
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
                BCore.MiniGameManager.CreateGame(new BackGammon(), bPlr).Join(bPlr);

            }
        }
    }
}
