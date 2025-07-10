using BossFramework.BModels;

using Microsoft.Xna.Framework;
using TerrariaApi.Server;
using TrProtocol.Packets;
using TShockAPI;

namespace BossFramework.BHooks.HookHandlers
{
    public static class PlayerGreetHandler
    {
        public delegate void OnJoin(BEventArgs.BaseEventArgs args);
        public static event OnJoin Join;
        public static void OnGreetPlayer(GreetPlayerEventArgs args)
        {
            var tsPlr = TShock.Players[args.Who];
            if (tsPlr != null && tsPlr.Account is { } account)
            {
                if (DB.DBTools.Get<BPlayer>(account.ID) is { } bPlr)
                {
                    bPlr.TsPlayer = tsPlr;
                    tsPlr.SetData("Boss.BPlayer", bPlr);

                    bPlr.WaitingPing = true;
                    bPlr.SendPacket(new ResetItemOwner() { ItemSlot = BCore.StatusSender.PING_ITEM_SLOT });
                    bPlr.PingChecker.Start();

                    Terraria.NetMessage.SendData((int)PacketTypes.PlayerInfo, -1, -1, null, bPlr.Index);

                    TShock.Utils.Broadcast($"{">>".Color("B6D6A2")} {bPlr.Name} {"加入服务器".Color("B6D6A2")}", Color.White);
                    Join?.Invoke(new BEventArgs.BaseEventArgs(bPlr));
                    args.Handled = true;
                }
                else
                    tsPlr.Disconnect($"服务器内部错误, 请尝试重新进入");
            }
        }
    }
}
