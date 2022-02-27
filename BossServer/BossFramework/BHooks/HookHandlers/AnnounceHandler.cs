using Microsoft.Xna.Framework;
using TShockAPI;
using static OTAPI.Hooks.NetMessage;

namespace BossFramework.BHooks.HookHandlers
{
    public static class AnnounceHandler
    {
        public static void OnAnnounce(object sender, PlayerAnnounceEventArgs args)
        {
            if (!Terraria.Netplay.Disconnect && args.Text.ToString().Contains("已离开") || args.Text.ToString().ToLower().Contains("has left"))
            {
                try
                {
                    var plrName = Terraria.Netplay.Clients[args.Plr].Name;
                    TShock.Utils.Broadcast($"{">>".Color("C8B592")} {plrName} {"离开服务器".Color("C8B592")}", Color.White);
                }
                catch { }
            }
            args.Result = PlayerAnnounceResult.WriteToConsole;
        }
    }
}
