using BossFramework.BInterfaces;
using BossFramework.BModels;
using EnchCoreApi.TrProtocol.NetPackets;

namespace BossFramework.BNet.PacketHandlers
{
    public class ItemOwnerHandler : PacketHandlerBase<ItemOwner>
    {
        public override bool OnGetPacket(BPlayer plr, ItemOwner packet)
        {
            if (packet.ItemSlot == BCore.StatusSender.PING_ITEM_SLOT)
            {
                BCore.StatusSender.GetPingBackPacket(plr);
                return false;
            }
            return base.OnGetPacket(plr, packet);
        }

        public override bool OnSendPacket(BPlayer plr, ItemOwner packet)
        {
            return base.OnSendPacket(plr, packet);
        }
    }
}
