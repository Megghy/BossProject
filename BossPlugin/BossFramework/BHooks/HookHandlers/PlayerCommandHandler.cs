using TShockAPI.Hooks;

namespace BossFramework.BHooks.HookHandlers
{
    public static class PlayerCommandHandler
    {
        public static void OnPlayerCommand(PlayerCommandEventArgs args)
        {
            if (args.Player == null)
            {
                return;
            }


        }
    }
}