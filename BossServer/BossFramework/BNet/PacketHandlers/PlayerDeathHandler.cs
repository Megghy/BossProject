using BossFramework.BInterfaces;
using BossFramework.BModels;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Localization;
using TrProtocol.Packets;
using TShockAPI;

namespace BossFramework.BNet.PacketHandlers
{
    public class PlayerDeathHandler : PacketHandlerBase<PlayerDeathV2>
    {
        public override bool OnGetPacket(BPlayer plr, PlayerDeathV2 packet)
        {
            var death = TShock.Players[packet.PlayerSlot]?.GetBPlayer();
            if (death is null || death.TrPlayer.dead)
                return true;
            var targetName = packet.Reason._sourcePlayerIndex is < 255 and >= 0
                ? TShock.Players[packet.Reason._sourcePlayerIndex]?.GetBPlayer()?.Name.Color("E5C5C5")
                : packet.Reason._sourceNPCIndex > 0
                ? Main.npc[packet.Reason._sourceNPCIndex].FullName.Color("A2B9D6")
                : "unknown".Color("E5C5C5");
            Item item = null;
            if (packet.Reason._sourceItemType > 0)
            {
                item = new();
                item.SetDefaults(packet.Reason._sourceItemType);
                item.prefix = (byte)packet.Reason._sourceItemPrefix;
            }
            string projName = null;
            if (packet.Reason._sourceProjectileType > 0)
            {
                projName = Lang.GetProjectileName(packet.Reason._sourceProjectileType).ToString();
            }
            string text;
            if (packet.Reason._sourcePlayerIndex is < 255 and >= 0
                || packet.Reason._sourceItemType > 0
                || packet.Reason._sourceProjectileType > 0)
                text = $">{(item is null ? "" : " " + TShock.Utils.ItemTag(item)) + " "}" +
                    $"{death.Name.Color("C8E1C7")} " +
                    $"被 {targetName} " +
                    $"{(projName is null ? "" : $"的 {projName}")} " +
                    $"杀死了";
            else
                text = $">{GetOtherDeathMsg(packet.Reason._sourceOtherIndex, death.Name).Replace(death.Name, $" {death.Name.Color("C8E1C7")} ")}";
            packet.Reason._sourceCustomReason = text;

            death.CurrentRegion?.GetPlayers().ForEach(p => p.SendMsg(text, new Color(190, 110, 110)));
            packet.SendPacketToAll();

            death.TrPlayer.dead = true;
            death.TsPlayer.Dead = true;
            death.TsPlayer.RespawnTimer = TShock.Config.Settings.RespawnSeconds;

            foreach (NPC npc in Main.npc)
            {
                if (npc.active && (npc.boss || npc.type == 13 || npc.type == 14 || npc.type == 15) &&
                    Math.Abs(death.TrPlayer.Center.X - npc.Center.X) + Math.Abs(death.TrPlayer.Center.Y - npc.Center.Y) < 4000f)
                {
                    death.TsPlayer.RespawnTimer = TShock.Config.Settings.RespawnBossSeconds;
                    break;
                }
            }
            base.OnGetPacket(plr, packet);
            return true;
        }

        public override bool OnSendPacket(BPlayer plr, PlayerDeathV2 packet)
        {
            return base.OnSendPacket(plr, packet);
        }
        private static string GetOtherDeathMsg(int index, string name)
        {
            NetworkText result = NetworkText.FromKey(Language.RandomFromCategory("DeathTextGeneric").Key, name, Main.worldName); ;
            switch (index)
            {
                case 0:
                    result = NetworkText.FromKey("DeathText.Fell_" + (Main.rand.Next(2) + 1), name);
                    break;
                case 1:
                    result = NetworkText.FromKey("DeathText.Drowned_" + (Main.rand.Next(4) + 1), name);
                    break;
                case 2:
                    result = NetworkText.FromKey("DeathText.Lava_" + (Main.rand.Next(4) + 1), name);
                    break;
                case 3:
                    result = NetworkText.FromKey("DeathText.Default", result);
                    break;
                case 4:
                    result = NetworkText.FromKey("DeathText.Slain", result);
                    break;
                case 5:
                    result = NetworkText.FromKey("DeathText.Petrified_" + (Main.rand.Next(4) + 1), name);
                    break;
                case 6:
                    result = NetworkText.FromKey("DeathText.Stabbed", name);
                    break;
                case 7:
                    result = NetworkText.FromKey("DeathText.Suffocated", name);
                    break;
                case 8:
                    result = NetworkText.FromKey("DeathText.Burned", name);
                    break;
                case 9:
                    result = NetworkText.FromKey("DeathText.Poisoned", name);
                    break;
                case 10:
                    result = NetworkText.FromKey("DeathText.Electrocuted", name);
                    break;
                case 11:
                    result = NetworkText.FromKey("DeathText.TriedToEscape", name);
                    break;
                case 12:
                    result = NetworkText.FromKey("DeathText.WasLicked", name);
                    break;
                case 13:
                    result = NetworkText.FromKey("DeathText.Teleport_1", name);
                    break;
                case 14:
                    result = NetworkText.FromKey("DeathText.Teleport_2_Male", name);
                    break;
                case 15:
                    result = NetworkText.FromKey("DeathText.Teleport_2_Female", name);
                    break;
                case 16:
                    result = NetworkText.FromKey("DeathText.Inferno", name);
                    break;
                case 17:
                    result = NetworkText.FromKey("DeathText.DiedInTheDark", name);
                    break;
                case 18:
                    result = NetworkText.FromKey("DeathText.Starved", name);
                    break;
                case 254:
                    //result = result;
                    break;
                case 255:
                    result = NetworkText.FromKey("DeathText.Slain", name);
                    break;
            }
            return result.ToString();
        }
    }
}
