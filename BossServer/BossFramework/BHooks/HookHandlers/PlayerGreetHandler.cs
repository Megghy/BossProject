using BossFramework.BModels;
using System.Linq;
using TerrariaApi.Server;
using TShockAPI;

namespace BossFramework.BHooks.HookHandlers
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
                tsPlr.SetData("Boss.BPlayer", bPlr);
            }
        }
    }
}
