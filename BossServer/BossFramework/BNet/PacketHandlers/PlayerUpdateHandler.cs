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
    public class PlayerUpdateHandler : PacketHandlerBase<SyncPlayer>
    {
        public override bool OnGetPacket(BPlayer plr, SyncPlayer packet)
        {
            packet.
        }

        public override bool OnSendPacket(BPlayer plr, SyncPlayer packet)
        {
            return false;
        }
    }
}
