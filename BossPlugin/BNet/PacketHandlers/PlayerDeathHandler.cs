using BossPlugin.BInterfaces;
using BossPlugin.BModels;
using TrProtocol.Packets;

namespace BossPlugin.BNet.PacketHandlers
{
    public class PlayerDeathHandler : PacketHandlerBase<PlayerDeathV2>
    {
        public override PacketTypes Type => PacketTypes.PlayerDeathV2;

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
