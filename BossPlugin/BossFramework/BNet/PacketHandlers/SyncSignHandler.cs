using BossFramework.BInterfaces;
using BossFramework.BModels;
using TrProtocol.Packets;
using TShockAPI;

namespace BossFramework.BNet.PacketHandlers
{
    public class SyncSignHandler : PacketHandlerBase<ReadSign>
    {
        public override bool OnGetPacket(BPlayer plr, ReadSign packet)
        {
            var args = new GetDataHandlers.SignEventArgs
            {
                Player = plr.TSPlayer,
                Data = null,
                ID = packet.SignSlot,
                X = packet.Position.X,
                Y = packet.Position.Y,
            };
            GetDataHandlers.Sign.Invoke(BossPlugin.Instance, args);
            if (!args.Handled)
                BCore.SignRedirector.OnSyncSign(plr, packet);
            return true; //不让其被otapi handle
        }

        public override bool OnSendPacket(BPlayer plr, ReadSign packet)
        {
            return base.OnSendPacket(plr, packet);
        }
    }
}
