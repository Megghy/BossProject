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
    public class DestroyProjectileHandler : PacketHandlerBase<KillProjectile>
    {
        public override bool OnGetPacket(BPlayer plr, KillProjectile packet)
        {
            BCore.ProjRedirect.OnProjDestory(plr, packet);
            return true;
        }

        public override bool OnSendPacket(BPlayer plr, KillProjectile packet)
        {
            return false;
        }
    }
}
