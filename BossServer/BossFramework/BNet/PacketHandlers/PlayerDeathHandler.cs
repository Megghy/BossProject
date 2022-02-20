using BossFramework.BInterfaces;
using BossFramework.BModels;
using Microsoft.Xna.Framework;
using System.Linq;
using TrProtocol.Packets;
using TShockAPI;

namespace BossFramework.BNet.PacketHandlers
{
    public class PlayerDeathHandler : PacketHandlerBase<PlayerDeathV2>
    {
        public override bool OnGetPacket(BPlayer plr, PlayerDeathV2 packet)
        {
            var targetName = packet.Reason._sourceOtherIndex is < 255 and >= 0
                ? TShock.Players[packet.Reason._sourceOtherIndex]?.GetBPlayer()?.Name.Color("E5C5C5")
                : packet.Reason._sourceNPCIndex > 0
                ? Terraria.Main.npc[packet.Reason._sourceNPCIndex].FullName.Color("A2B9D6")
                : "unknown".Color("E5C5C5");
            Terraria.Item item = null;
            if(packet.Reason._sourceItemType > 0)
            {
                item = new();
                item.SetDefaults(packet.Reason._sourceItemType);
                item.prefix = (byte)packet.Reason._sourceItemPrefix;
            }
            string projName = null;
            if(packet.Reason._sourceProjectileType > 0)
            {
                projName = Terraria.Lang.GetProjectileName(packet.Reason._sourceProjectileType).ToString();
            }
            var text = $"{(item is null ? "" : TShock.Utils.ItemTag(item)) + " "}" +
                $"{plr.Name.Color("C8E1C7")} " +
                $"被 {targetName} " +
                $"{(projName is null ? "" : $"的 {projName}")}" +
                $"杀死了";
            packet.Reason._sourceCustomReason = text;
            plr.CurrentRegion?.GetPlayers().ForEach(p =>
            {
                p.SendMsg(text, new Color(190, 110, 110));
                p.SendPacket(packet);
            });
            return true;
        }

        public override bool OnSendPacket(BPlayer plr, PlayerDeathV2 packet)
        {
            return false;
        }
    }
}
