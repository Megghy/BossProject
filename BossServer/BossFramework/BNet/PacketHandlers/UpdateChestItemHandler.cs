using BossFramework.BInterfaces;
using BossFramework.BModels;
using TrProtocol.Packets;
using TShockAPI;

namespace BossFramework.BNet.PacketHandlers
{
    public class UpdateChestItemHandler : PacketHandlerBase<SyncChestItem>
    {
        public override bool OnGetPacket(BPlayer plr, SyncChestItem packet)
        {
            var args = new GetDataHandlers.ChestItemEventArgs()
            {
                Player = plr.TsPlayer,
                Data = null,
                ID = packet.ChestSlot,
                Prefix = packet.Prefix,
                Slot = packet.ChestItemSlot,
                Stacks = packet.Stack,
                Type = packet.ItemType
            };
            GetDataHandlers.ChestItemChange.Invoke(BossPlugin.Instance, args);
            if (!args.Handled)
                BCore.ChestRedirector.OnUpdateChestItem(plr, packet);
            return true;
        }

        public override bool OnSendPacket(BPlayer plr, SyncChestItem packet)
        {
            return base.OnSendPacket(plr, packet);
        }
    }
}
