using Microsoft.Xna.Framework;
using TerrariaApi.Server;
using TShockAPI;

namespace BossFramework.BHooks.HookHandlers
{
    public class PlayerLeaveHandler
    {
        public static void OnPlayerLeave(LeaveEventArgs args)
        {
            var bPlr = TShock.Players[args.Who].GetBPlayer();
            TShock.Utils.Broadcast($"{">>".Color("C8B592")} {bPlr.Name} {"离开服务器".Color("C8B592")}", Color.White);

        }
    }
}
