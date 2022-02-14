using Microsoft.Xna.Framework;
using TrProtocol;
using TrProtocol.Packets;

namespace BossFramework.BModels
{
    public interface IEventArgs
    {
        public bool Handled { get; set; }
        public BPlayer Player { get; }
    }
    public static class BEventArgs
    {
        public class PacketEventArgs : IEventArgs
        {
            public PacketEventArgs(BPlayer plr, Packet packet)
            {
                Player = plr;
                Packet = packet;
            }
            public BPlayer Player { get; private set; }
            public Packet Packet { get; set; }
            public bool Handled { get; set; } = false;
        }
        public class ProjCreateEventArgs : IEventArgs
        {
            public ProjCreateEventArgs(SyncProjectile proj, BPlayer plr)
            {
                Proj = proj;
                Player = plr;
            }
            public SyncProjectile Proj { get; set; }
            public BPlayer Player { get; set; }
            public bool Handled { get; set; } = false;
        }
        public class ProjDestroyEventArgs
        {
            public ProjDestroyEventArgs(KillProjectile killProj, BPlayer plr)
            {
                KillProj = killProj;
                Player = plr;
            }
            public KillProjectile KillProj { get; set; }
            public BPlayer Player { get; set; }
        }
        public struct BRegionEventArgs
        {
            public BRegionEventArgs(BRegion region, BPlayer plr)
            {
                Region = region;
                Player = plr;
            }
            public BRegion Region { get; set; }
            public BPlayer Player { get; set; }
        }
        public class PlayerDamageEventArgs : IEventArgs
        {
            public PlayerDamageEventArgs(PlayerHurtV2 hurt, BPlayer plr)
            {
                Hurt = hurt;
                Player = plr;
            }
            public PlayerHurtV2 Hurt { get; set; }
            public BPlayer Player { get; set; }
            public bool Handled { get; set; } = false;
        }
        public class SignCreateEventArgs : IEventArgs
        {
            public SignCreateEventArgs(BPlayer plr, Point position)
            {
                Player = plr;
                Position = position;
            }
            public bool Handled { get; set; } = false;
            public BPlayer Player { get;private set; }
            public Point Position { get;private set; }
        }
        public class SignUpdateEventArgs : IEventArgs
        {
            public SignUpdateEventArgs(BPlayer plr, BSign sign, string newText)
            {
                Player = plr;
                Sign = sign;
                NewText = newText;
            }
            public bool Handled { get; set; } = false;
            public BPlayer Player { get; private set; }
            public BSign Sign { get; private set; }
            public string NewText { get; private set; }
        }
        public class SignRmoveEventArgs
        {
            public SignRmoveEventArgs(BPlayer plr, BSign sign)
            {
                Player = plr;
                Sign = sign;
            }
            public bool Handled { get; set; } = false;
            public BPlayer Player { get; private set; }
            public BSign Sign { get; private set; }
        }
    }
}
