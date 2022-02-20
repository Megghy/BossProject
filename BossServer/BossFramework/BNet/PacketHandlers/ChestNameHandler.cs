using BossFramework.BInterfaces;
using BossFramework.BModels;
using TrProtocol.Packets;

namespace BossFramework.BNet.PacketHandlers
{
    public class aaaa : PacketHandlerBase<WorldData>
    {
        public override bool OnGetPacket(BPlayer plr, WorldData packet)
        {
            return false;
        }

        public override bool OnSendPacket(BPlayer plr, WorldData packet)
        {
            return false;
        }
    }
    public class ChestNameHandler : PacketHandlerBase<ChestName>
    {
        public override bool OnGetPacket(BPlayer plr, ChestName packet)
        {
            BCore.ChestRedirector.OnGetName(plr, packet);
            return true;
        }

        public override bool OnSendPacket(BPlayer plr, ChestName packet)
        {
            return true;
        }
    }
}
