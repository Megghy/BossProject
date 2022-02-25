using TerrariaApi.Server;

namespace BossFramework.BHooks.HookHandlers
{
    public static class PlayerConnectHandler
    {
        public static void OnConnect(ConnectEventArgs args)
        {
            if (Terraria.Netplay.Clients[args.Who].State != 0)
            {
                Terraria.NetMessage.TrySendData(2, args.Who);
                return;
            }
            if (string.IsNullOrEmpty(Terraria.Netplay.ServerPassword))
            {
                Terraria.Netplay.Clients[args.Who].State = 1;
                Terraria.NetMessage.TrySendData(3, args.Who);
            }
            else
            {
                Terraria.Netplay.Clients[args.Who].State = -1;
                Terraria.NetMessage.TrySendData(37, args.Who);
            }
        }
    }
}
