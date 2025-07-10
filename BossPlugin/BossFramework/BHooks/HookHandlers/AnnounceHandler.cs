using Microsoft.Xna.Framework;
using TShockAPI;
using static OTAPI.Hooks.NetMessage;

namespace BossFramework.BHooks.HookHandlers
{
    public static class AnnounceHandler
    {
        public static void OnAnnounce(object sender, PlayerAnnounceEventArgs args)
        {
            if (!Terraria.Netplay.Disconnect && (args.Text.ToString().EndsWith("已离开") || args.Text.ToString().EndsWith("离开了游戏。") || args.Text.ToString().EndsWith("has left.", StringComparison.CurrentCultureIgnoreCase)))
            {
                try
                {
                    var plrName = Terraria.Netplay.Clients[args.Plr].Name;
                    TShock.Utils.Broadcast($"{">>".Color("C8B592")} {plrName} {"离开服务器".Color("C8B592")}", Color.White);
                    args.Result = PlayerAnnounceResult.None;
                }
                catch { }
            }
            args.Result = PlayerAnnounceResult.WriteToConsole;
        }
    }
}
