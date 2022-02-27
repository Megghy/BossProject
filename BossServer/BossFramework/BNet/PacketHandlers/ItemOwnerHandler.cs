using BossFramework.BInterfaces;
using BossFramework.BModels;
using TrProtocol.Packets;

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
            return false;
        }

        public override bool OnSendPacket(BPlayer plr, ItemOwner packet)
        {
            return false;
        }
    }
}
