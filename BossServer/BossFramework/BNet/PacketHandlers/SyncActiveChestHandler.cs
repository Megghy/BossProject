using BossFramework.BInterfaces;
using BossFramework.BModels;
using TrProtocol.Packets;

namespace BossFramework.BNet.PacketHandlers
{
    internal class SyncActiveChestHandler : PacketHandlerBase<SyncPlayerChest>
    {
        public override bool OnGetPacket(BPlayer plr, SyncPlayerChest packet)
        {
            BCore.ChestRedirector.OnSyncActiveChest(plr, packet);
            return true;
        }

        public override bool OnSendPacket(BPlayer plr, SyncPlayerChest packet)
        {
            return false;
        }
    }
}
