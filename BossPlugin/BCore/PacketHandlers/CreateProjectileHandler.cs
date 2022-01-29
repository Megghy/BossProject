using BossPlugin.BInterfaces;
using BossPlugin.BModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TrProtocol.Packets;

namespace BossPlugin.BCore.PacketHandlers
{
    public class CreateProjectileHandler : PacketHandlerBase<SyncProjectile>
    {
        public override PacketTypes Type => PacketTypes.ProjectileNew;

        public override bool OnGetPacket(BPlayer plr, SyncProjectile packet)
        {
            return ProjRedirect.OnProjCreate(plr, packet);
        }

        public override bool OnSendPacket(BPlayer plr, SyncProjectile packet)
        {
            return false;
        }
    }
}
