using BossFramework.BInterfaces;
using BossFramework.BModels;
using TrProtocol.Packets;


namespace BossFramework.BNet.PacketHandlers
{
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
