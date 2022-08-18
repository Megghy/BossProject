using BossFramework.BInterfaces;
using BossFramework.BModels;
using TrProtocol.Packets;

namespace BossFramework.BNet.PacketHandlers
{
    public class SyncProjectileHandler : PacketHandlerBase<SyncProjectile>
    {
        public override bool OnGetPacket(BPlayer plr, SyncProjectile packet)
        {
            return base.OnGetPacket(plr, packet);
        }

        public override bool OnSendPacket(BPlayer plr, SyncProjectile packet)
        {
            return base.OnSendPacket(plr, packet);
        }
    }
}
