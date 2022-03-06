using static OTAPI.Hooks.Netplay;

namespace BossFramework.BHooks.HookHandlers
{
    public static class SocketCreateHandler
    {
        public static void OnSocketCreate(object o, CreateTcpListenerEventArgs args)
        {
            args.Result = new BNet.BossSocket();
        }
    }
}
