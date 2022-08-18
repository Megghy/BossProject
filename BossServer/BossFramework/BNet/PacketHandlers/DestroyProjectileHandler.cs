using BossFramework.BInterfaces;
using BossFramework.BModels;
using TrProtocol.Packets;

namespace BossFramework.BNet.PacketHandlers
{
    public class DestroyProjectileHandler : PacketHandlerBase<KillProjectile>
    {
        public override bool OnGetPacket(BPlayer plr, KillProjectile packet)
        {
            return base.OnGetPacket(plr, packet);
        }

        public override bool OnSendPacket(BPlayer plr, KillProjectile packet)
        {
            return base.OnSendPacket(plr, packet);
        }
    }
}
