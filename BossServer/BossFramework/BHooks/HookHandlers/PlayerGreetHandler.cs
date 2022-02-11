using BossFramework.BCore;
using BossFramework.BModels;
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
                var bPlr = DB.DBTools.Get<BPlayer>(account.ID);
                bPlr.TsPlayer = tsPlr;
                tsPlr.SetData("Boss.BPlayer", bPlr);

                bPlr.ChangeCustomWeaponMode(true);
            }
        }
    }
}
