using BossFramework.BInterfaces;
using BossFramework.BModels;
using EnchCoreApi.TrProtocol.NetPackets;

namespace BossFramework.BNet.PacketHandlers
{
    public class UpdateChestHandler : PacketHandlerBase<ChestUpdates>
    {
        public override bool OnGetPacket(BPlayer plr, ChestUpdates packet)
        {
            BCore.ChestRedirector.OnPlaceOrDestroyChest(plr, packet);
            return base.OnGetPacket(plr, packet);
        }

        public override bool OnSendPacket(BPlayer plr, ChestUpdates packet)
        {
            return base.OnSendPacket(plr, packet);
        }
    }
}
