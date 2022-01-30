using BossPlugin.BModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TerrariaApi.Server;
using TShockAPI;

namespace BossPlugin.BCore.HookHandlers
{
    public static class PlayerGreetHandler
    {
        public static void OnGreetPlayer(GreetPlayerEventArgs args)
        {
            var tsPlr = TShock.Players[args.Who];
            if (tsPlr != null && tsPlr.Account is { } account)
            { 
                tsPlr.SetData("BossPlugin.BPlayer", DB.DBTools.GetSingle<BPlayer>(account.ID.ToString()));
            }
        }
    }
}
