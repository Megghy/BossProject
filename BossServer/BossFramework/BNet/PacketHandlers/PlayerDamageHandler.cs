using BossFramework.BInterfaces;
using BossFramework.BModels;
using EnchCoreApi.TrProtocol.NetPackets;

namespace BossFramework.BNet.PacketHandlers
{
    public class PlayerDamageHandler : PacketHandlerBase<PlayerHurtV2>
    {
        public override bool OnGetPacket(BPlayer plr, PlayerHurtV2 packet)
        {
            return base.OnGetPacket(plr, packet);
        }

        public override bool OnSendPacket(BPlayer plr, PlayerHurtV2 packet)
        {
            return base.OnSendPacket(plr, packet);
        }
    }
}
