namespace BossWeb.Core
{
    public static class Initializer
    {
        public static void Init()
        {
            //TerrariaApi.Server.Program.Main([]);
            if (Config.Instance.AutoStart)
            {
                ServerManager.StartServer();
                Utils.NotificateInfo("[BossWeb] 服务器正在启动");
            }
        }
    }
}
