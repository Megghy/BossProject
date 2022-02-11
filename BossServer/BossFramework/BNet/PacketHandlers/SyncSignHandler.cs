using BossFramework.BInterfaces;
using BossFramework.BModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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
                Player = plr.TsPlayer,
                Data = null,
                ID = packet.SignSlot,
                X = packet.Position.X,
                Y = packet.Position.Y,
            };
            GetDataHandlers.Sign.Invoke(BPlugin.Instance, args);
            if (!args.Handled)
                BCore.SignRedirector.OnSyncSign(plr, packet);
            return true; //不让其被otapi handle
        }

        public override bool OnSendPacket(BPlayer plr, ReadSign packet)
        {
            return false;
        }
    }
}
