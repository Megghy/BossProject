using BossPlugin.BAttributes;
using TerrariaApi.Server;
using TShockAPI.Hooks;

namespace BossPlugin.BHooks
{
    public static class HookHandler
    {
        [AutoInit("挂载所有Hook")]
        private static void HandleHooks()
        {
            GeneralHooks.ReloadEvent += HookHandlers.ReloadHandler.OnReload;
            AccountHooks.AccountCreate += HookHandlers.CreateAccountHandler.OnCreateAccount;

            ServerApi.Hooks.NetGreetPlayer.Register(BPlugin.Instance, HookHandlers.PlayerGreetHandler.OnGreetPlayer);
            ServerApi.Hooks.ServerLeave.Register(BPlugin.Instance, HookHandlers.PlayerLeaveHandler.OnPlayerLeave);

            ServerApi.Hooks.NetGetData.Register(BPlugin.Instance, BNet.PacketHandler.OnGetData);
        }
    }
}
