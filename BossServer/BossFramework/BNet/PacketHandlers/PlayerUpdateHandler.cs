using BossFramework.BInterfaces;
using BossFramework.BModels;
using TrProtocol.Packets;

namespace BossFramework.BNet.PacketHandlers
{
    public class PlayerUpdateHandler : PacketHandlerBase<SyncPlayer>
    {
        public override bool OnGetPacket(BPlayer plr, SyncPlayer packet)
        {
            return false;
        }

        public override bool OnSendPacket(BPlayer plr, SyncPlayer packet)
        {
            return false;
        }
    }
}
