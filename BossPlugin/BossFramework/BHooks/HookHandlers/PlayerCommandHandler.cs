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

            if (args.Player.ContainsData("MiniWorld.InWorld") && args.CommandText.StartsWith("//"))
            {
                args.Handled = true; // 阻止指令继续处理
            }
        }
    }
}