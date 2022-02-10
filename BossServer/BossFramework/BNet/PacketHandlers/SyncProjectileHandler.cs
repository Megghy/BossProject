using BossFramework.BInterfaces;
using BossFramework.BModels;
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
