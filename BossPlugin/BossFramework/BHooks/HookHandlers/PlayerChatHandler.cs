using TShockAPI.Hooks;

namespace BossFramework.BHooks.HookHandlers
{
    public static class PlayerChatHandler
    {
        public static void OnChat(PlayerChatEventArgs args)
        {
            if (!BConfig.Instance.EnableChatAboveHead)
                return;
        }
    }
}
