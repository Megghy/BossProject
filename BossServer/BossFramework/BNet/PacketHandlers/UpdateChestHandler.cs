using BossFramework.BInterfaces;
using BossFramework.BModels;
using TrProtocol.Packets;

namespace BossFramework.BNet.PacketHandlers
{
    public class UpdateChestHandler : PacketHandlerBase<ChestUpdates>
    {
        public override bool OnGetPacket(BPlayer plr, ChestUpdates packet)
        {
            BCore.ChestRedirector.OnPlaceOrDestroyChest(plr, packet);
            return false;
        }

        public override bool OnSendPacket(BPlayer plr, ChestUpdates packet)
        {
            return false;
        }
    }
}
