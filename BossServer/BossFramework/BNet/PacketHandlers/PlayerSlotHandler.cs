using BossFramework.BInterfaces;
using BossFramework.BModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TrProtocol.Packets;

namespace BossFramework.BNet.PacketHandlers
{
    public class PlayerSlotHandler : PacketHandlerBase<SyncEquipment>
    {
        public override bool OnGetPacket(BPlayer plr, SyncEquipment packet)
        {
            BCore.BWeaponSystem.CheckIncomeItem(plr, packet);
        }

        public override bool OnSendPacket(BPlayer plr, SyncEquipment packet)
        {
            return false;
        }
    }
}
