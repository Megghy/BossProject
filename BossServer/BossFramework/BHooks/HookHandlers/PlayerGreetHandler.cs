using BossFramework.BModels;
using TerrariaApi.Server;
using TrProtocol.Packets;
using TShockAPI;

namespace BossFramework.BHooks.HookHandlers
{
    public static class PlayerGreetHandler
    {
        public static void OnGreetPlayer(GreetPlayerEventArgs args)
        {
            var tsPlr = TShock.Players[args.Who];
            if (tsPlr != null && tsPlr.Account is { } account)
            {
                if (DB.DBTools.Get<BPlayer>(account.ID) is { } bPlr)
                {
                    bPlr.TsPlayer = tsPlr;
                    tsPlr.SetData("Boss.BPlayer", bPlr);
                    bPlr.SendPacket(new ResetItemOwner() { ItemSlot = BCore.StatusSender.PING_ITEM_SLOT });
                    bPlr.PingChecker.Start();
                }
                else
                    tsPlr.Disconnect($"服务器内部错误, 请尝试重新进入");
            }
        }
    }
}
