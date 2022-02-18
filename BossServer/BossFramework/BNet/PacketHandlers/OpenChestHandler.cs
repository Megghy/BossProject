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
    public class OpenChestHandler : PacketHandlerBase<RequestChestOpen>
    {

        public override bool OnGetPacket(BPlayer plr, RequestChestOpen packet)
        {
            var args = new GetDataHandlers.ChestOpenEventArgs()
            {
                Player = plr.TsPlayer,
                Data = null,
                X = packet.Position.X,
                Y = packet.Position.Y
            };
            GetDataHandlers.ChestOpen.Invoke(BossPlugin.Instance, args);
            if (!args.Handled)
                BCore.ChestRedirector.OnRequestChestOpen(plr, packet);
            return true;
        }

        public override bool OnSendPacket(BPlayer plr, RequestChestOpen packet)
        {
            return false;
        }
    }
}
