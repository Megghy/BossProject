using BossFramework.BInterfaces;
using BossFramework.BModels;
using TrProtocol.Packets;

namespace BossFramework.BNet.PacketHandlers
{
    public class PlayerDeathHandler : PacketHandlerBase<PlayerDeathV2>
    {
        public override bool OnGetPacket(BPlayer plr, PlayerDeathV2 packet)
        {
            if (plr.TsPlayer?.TPlayer.hostile == true)
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
