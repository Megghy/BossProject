using BossFramework.BInterfaces;
using BossFramework.BModels;
using TrProtocol.Packets;

namespace BossFramework.BNet.PacketHandlers
{
    public class PlayerSlotHandler : PacketHandlerBase<SyncEquipment>
    {
        public override bool OnGetPacket(BPlayer plr, SyncEquipment packet)
        {
            if (packet.ItemSlot == 58)
                plr.ItemInHand = new(packet.ItemType, packet.Stack, packet.Prefix);
            if (BCore.BWeaponSystem.CheckIncomeItem(plr, packet))
                return true;
            return false;
        }

        public override bool OnSendPacket(BPlayer plr, SyncEquipment packet)
        {
            if (plr.IsChangingWeapon)
                return true;
            return false;
        }
    }
}
