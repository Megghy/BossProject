using BossFramework.BCore;
using BossFramework.BInterfaces;
using BossFramework.BModels;
using TrProtocol.Packets;

namespace BossFramework.BNet.PacketHandlers
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
