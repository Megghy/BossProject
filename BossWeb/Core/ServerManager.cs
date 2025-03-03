using System.Reflection;
using TerrariaApi.Server;
using TShockAPI;

namespace BossWeb.Core
{
    public static class ServerManager
    {
        class FakePlugin(Terraria.Main game) : TerrariaPlugin(game)
        {
            public override void Initialize()
            {
            }
        }
        public enum ServerStatus { Stopped, Starting, Running, Stopping }
        public static ServerStatus Status { get; private set; } = ServerStatus.Stopped;
        public static Assembly TSAPIAssembly { get; private set; } = typeof(ServerApi).Assembly;
        public static Thread ServerThread { get; private set; }
        public static bool StartServer()
        {
            try
            {
                // string to args
                var args = Config.Instance.StartupCommandLine.Split(' ');

                ServerThread = new Thread(new ThreadStart(() =>
                {
                    try
                    {
                        typeof(ServerApi).Assembly.EntryPoint!.Invoke(null, [args.Concat(Datas.Args).ToArray()]);
                        //TerrariaApi.Server.Program.Main(args);
                        Utils.NotificateSuccess("服务器已启动");
                    }
                    catch (Exception ex)
                    {
                        Utils.NotificateError("无法启动服务器", ex.Message);
                    }
                }))
                {
                    IsBackground = true
                };
                Status = ServerStatus.Starting;
                ServerThread.Start();

                ServerApi.Hooks.GamePostInitialize.Register(new FakePlugin(null), PostServerInitialized, -1);  //注册服务器加载完成的钩子

                Utils.NotificateInfo("[BossWeb] 服务器正在启动");
                return true;
            }
            catch (Exception e)
            {
                TShock.Log?.Error(e.Message);
                Utils.NotificateError("无法启动服务器", e.Message);
                Status = ServerStatus.Stopped;
                return false;
            }
        }
        public static void StopServer()
        {
            Status = ServerStatus.Stopping;
            ServerApi.Hooks.GamePostInitialize.Deregister(new FakePlugin(null), PostServerInitialized);
            Utils.NotificateSuccess("[BossWeb] 服务器已关闭");
        }

        private static void PostServerInitialized(EventArgs args)
        {
            Status = ServerStatus.Running;
            Utils.NotificateSuccess("服务器启动完成");
        }
    }
}
