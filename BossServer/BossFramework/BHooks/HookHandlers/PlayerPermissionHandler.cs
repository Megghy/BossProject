using TShockAPI.Hooks;

namespace BossFramework.BHooks.HookHandlers
{
    internal class PlayerPermissionHandler
    {
        public static void OnPlayerPermission(PlayerPermissionEventArgs args)
        {
            if (args.Player.GetData<bool>("BossFramework.IgnorePerm"))
            {
                args.Result = PermissionHookResult.Granted;
            }
        }
    }
}
