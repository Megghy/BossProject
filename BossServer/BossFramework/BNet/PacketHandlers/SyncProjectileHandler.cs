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
    public class SyncProjectileHandler : PacketHandlerBase<SyncProjectile>
    {
        public override bool OnGetPacket(BPlayer plr, SyncProjectile packet)
        {
            BCore.ProjRedirect.SyncProjsQueue.Enqueue((plr, packet));
            return true;
        }

        public override bool OnSendPacket(BPlayer plr, SyncProjectile packet)
        {
            return false;
        }
    }
}
