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
    public class UpdateChestHandler : PacketHandlerBase<ChestUpdates>
    {
        public override bool OnGetPacket(BPlayer plr, ChestUpdates packet)
        {
            BCore.ChestRedirector.OnPlaceOrDestroyChest(plr, packet);
            return false;
        }

        public override bool OnSendPacket(BPlayer plr, ChestUpdates packet)
        {
            return false;
        }
    }
}
