using EnchCoreApi.TrProtocol.NetPackets;
using Microsoft.Xna.Framework;
using Terraria;
using TShockAPI;

namespace Nanami
{
    internal class NanamiListener : IDisposable
    {
        public NanamiListener()
        {
            BossFramework.BNet.PacketHandlers.PlayerDamageHandler.Get += OnPlayerDamage;
            BossFramework.BNet.PacketHandlers.PlayerDeathHandler.Get += OnKillMe;
        }

        public void Dispose()
        {
            BossFramework.BNet.PacketHandlers.PlayerDamageHandler.Get -= OnPlayerDamage;
            BossFramework.BNet.PacketHandlers.PlayerDeathHandler.Get -= OnKillMe;
        }

        private static void OnPlayerDamage(BossFramework.BModels.BEventArgs.PacketHookArgs<PlayerHurtV2> args)
        {
            // 记录 伤害量
            var data = PlayerPvpData.GetPlayerData(args.Player.TSPlayer);

            var calculatedDmg = (int)Main.CalculateDamagePlayersTakeInPVP(args.Packet.Damage, Main.player[args.Packet.OtherPlayerSlot].statDefense);

            data.Damage(calculatedDmg);

            // 记录 承受伤害量
            PlayerPvpData.GetPlayerData(args.Packet.OtherPlayerSlot).Hurt(calculatedDmg);
        }

        private static void OnKillMe(BossFramework.BModels.BEventArgs.PacketHookArgs<PlayerDeathV2> args)
        {
            if (!args.Player.TRPlayer.hostile)
            {
                return;
            }

            args.Player.TSPlayer.RespawnTimer = Nanami.Config.RespawnPvPSeconds;
            var data = PlayerPvpData.GetPlayerData(args.Player.TSPlayer);

            // 处理死亡事件
            data.Die(args.Packet.Damage);

            var killer = args.Packet.Reason._sourcePlayerIndex;

            // 处理杀死事件
            if (killer >= 0 && killer < 256)
            {
                var killerData = PlayerPvpData.GetPlayerData(killer);
                if (killerData.Kill() is { } killMessage)
                {
                    args.Player.CurrentRegion?.GetPlayers().ForEach(p => p.SendMsg(killMessage, new Color(190, 110, 110)));
                    //TSPlayer.All.SendMessage(killMessage, color: Color.White);
                }
            }

            //args.PlayerDeathReason._sourceCustomReason = args.Player.Name + deathText;

            //Main.player[args.PlayerId].KillMe(args.PlayerDeathReason, args.Damage, args.Direction, true);
            //NetMessage.SendPlayerDeath(args.PlayerId, args.PlayerDeathReason, args.Damage, args.Direction, true, -1, args.Player.Index);

            args.Handled = true;
        }
    }
}
