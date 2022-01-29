using BossPlugin.BInterfaces;
using BossPlugin.BModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TrProtocol;
using TrProtocol.Packets;

namespace BossPlugin.BCore.PacketHandlers
{
    public class PlayerDeathHandler : PacketHandlerBase<PlayerDeathV2>
    {
        public override PacketTypes Type => PacketTypes.PlayerDeathV2;

        public override bool OnGetPacket(BPlayer plr, PlayerDeathV2 packet)
        {
            if(plr.Player.TPlayer.hostile)
            {
                return true;
            }
            return false;
        }

        public override bool OnSendPacket(BPlayer plr, PlayerDeathV2 packet)
        {
            return false;
        }
    }
}
