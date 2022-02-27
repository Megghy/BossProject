using BossFramework.BInterfaces;
using BossFramework.BModels;
using TrProtocol.Packets;

namespace BossFramework.BNet.PacketHandlers
{
    public class PlayerDamageHandler : PacketHandlerBase<PlayerHurtV2>
    {
        public delegate void OnPlayerDamage(BEventArgs.PlayerDamageEventArgs hurt);
        public static event OnPlayerDamage PlayerDamage;
        public override bool OnGetPacket(BPlayer plr, PlayerHurtV2 packet)
        {
            var args = new BEventArgs.PlayerDamageEventArgs(packet, plr);
            PlayerDamage?.Invoke(args);
            return args.Handled;
        }

        public override bool OnSendPacket(BPlayer plr, PlayerHurtV2 packet)
        {
            return false;
        }
    }
}
