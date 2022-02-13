using BossFramework.BInterfaces;
using BossFramework.BModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TrProtocol.Packets;

namespace BossFramework.BNet.PacketHandlers
{
    public class ChestNameHandler : PacketHandlerBase<ChestName>
    {
        public override bool OnGetPacket(BPlayer plr, ChestName packet)
        {
            BCore.ChestRedirector.OnGetName(plr, packet);
            return true;
        }

        public override bool OnSendPacket(BPlayer plr, ChestName packet)
        {
            return true;
        }
    }
}
