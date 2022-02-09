using BossFramework.BAttributes;
using TerrariaApi.Server;
using TShockAPI.Hooks;

namespace BossFramework.BHooks
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
            ServerApi.Hooks.GamePostInitialize.Register(BPlugin.Instance, HookHandlers.PostInitializeHandler.OnGamePostInitialize, int.MinValue);
            ServerApi.Hooks.GameUpdate.Register(BPlugin.Instance, HookHandlers.GameUpdateHandler.OnGameUpdate);
            ServerApi.Hooks.NetGetData.Register(BPlugin.Instance, BNet.PacketHandler.OnGetData);
        }
    }
}
