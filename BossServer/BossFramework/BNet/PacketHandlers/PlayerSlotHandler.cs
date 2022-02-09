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
            BCore.BWeaponSystem.CheckIncomeItem(plr, packet);
            if(packet.ItemSlot < 59)
            {
                plr.TrPlayer.inventory[packet.ItemSlot] ??= new();
                plr.TrPlayer.inventory[packet.ItemSlot].SetDefaults(packet.ItemType);
                plr.TrPlayer.inventory[packet.ItemSlot].stack = packet.Stack;
                plr.TrPlayer.inventory[packet.ItemSlot].prefix = packet.Prefix;
            }    
            return false;
        }

        public override bool OnSendPacket(BPlayer plr, SyncEquipment packet)
        {
            return false;
        }
    }
}
