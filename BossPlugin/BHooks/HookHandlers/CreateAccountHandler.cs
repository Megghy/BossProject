using BossPlugin.BModels;
using System.Linq;
using TShockAPI;
using TShockAPI.Hooks;

namespace BossPlugin.BHooks.HookHandlers
{
    public static class CreateAccountHandler
    {
        public static void OnCreateAccount(AccountCreateEventArgs args)
        {
            var tsPlr = TShock.Players.FirstOrDefault(p => p?.Account?.Name == args.Account.Name);
            if (tsPlr != null)
            {
                var bPlr = new BPlayer(tsPlr);
                DB.DBTools.Insert(bPlr);
                tsPlr.SetData("BossPlugin.BPlayer", bPlr);
            }
        }
    }
}
