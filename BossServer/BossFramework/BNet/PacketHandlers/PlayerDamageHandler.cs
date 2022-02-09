using BossFramework.BInterfaces;
using BossFramework.BModels;
using TrProtocol.Packets;

namespace BossFramework.BNet.PacketHandlers
{
    public class PlayerDamageHandler : PacketHandlerBase<PlayerHurtV2>
    {
        public override bool OnGetPacket(BPlayer plr, PlayerHurtV2 packet)
        {
            BCore.BWeaponSystem.OnPlayerHurt(plr, packet);
            return false;
        }

        public override bool OnSendPacket(BPlayer plr, PlayerHurtV2 packet)
        {
            return false;
        }
    }
}
