using Microsoft.Xna.Framework;
using System.IO;
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
            public PacketEventArgs(BPlayer plr, PacketTypes type, BinaryReader reader)
            {
                Player = plr;
                PacketType = type;
                Reader = reader;
            }
            public PacketEventArgs(BPlayer plr, Packet packet)
            {
                Player = plr;
                PacketType = (PacketTypes)packet.Type;
                _packet = packet;
            }
            public PacketTypes PacketType { get; private set; }
            public BPlayer Player { get; private set; }
            private BinaryReader Reader { get; set; }
            private Packet _packet;
            /// <summary>
            /// 未确定是否要读取前只使用 <see cref="PacketType"/> 查看类型
            /// </summary>
            public Packet Packet
            {
                get
                {
                    _packet ??= BNet.PacketHandler.Serializer.Deserialize(Reader);
                    return _packet;
                }
            }
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
        public class SignReadEventArgs : IEventArgs
        {
            public SignReadEventArgs(BPlayer plr, Point position)
            {
                Player = plr;
                Position = position;
            }
            public bool Handled { get; set; } = false;
            public BPlayer Player { get; private set; }
            public Point Position { get; private set; }
        }
        public class SignCreateEventArgs : IEventArgs
        {
            public SignCreateEventArgs(BPlayer plr, Point position)
            {
                Player = plr;
                Position = position;
            }
            public bool Handled { get; set; } = false;
            public BPlayer Player { get; private set; }
            public Point Position { get; private set; }
        }
        public class SignUpdateEventArgs : IEventArgs
        {
            public SignUpdateEventArgs(BPlayer plr, ReadSign data)
            {
                Player = plr;
                Data = data;
            }
            public bool Handled { get; set; } = false;
            public BPlayer Player { get; private set; }
            public ReadSign Data { get; private set; }
            public Point Position
                => Data.Position.ToPoint();
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
        public class ChestOpenEventArgs : IEventArgs
        {
            public ChestOpenEventArgs(BPlayer plr, Point position)
            {
                Player = plr;
                Position = position;
            }
            public bool Handled { get; set; } = false;
            public BPlayer Player { get; private set; }
            public Point Position { get; private set; }
        }
        public class ChestCreateEventArgs : IEventArgs
        {
            public ChestCreateEventArgs(BPlayer plr, Point position)
            {
                Player = plr;
                Position = position;
            }
            public bool Handled { get; set; } = false;
            public BPlayer Player { get; private set; }
            public Point Position { get; private set; }
        }
        public class ChestUpdateItemEventArgs : IEventArgs
        {
            public ChestUpdateItemEventArgs(BPlayer plr, SyncChestItem data)
            {
                Player = plr;
                Data = data;
            }
            public bool Handled { get; set; } = false;
            public BPlayer Player { get; private set; }
            public SyncChestItem Data { get; private set; }
        }
        public class ChestRemoveEventArgs
        {
            public ChestRemoveEventArgs(BPlayer plr, Point position, BChest chest = null)
            {
                Player = plr;
                Position = position;
                Chest = chest;
            }
            public bool Handled { get; set; } = false;
            public BPlayer Player { get; private set; }
            public Point Position { get; private set; }
            public BChest Chest { get; private set; }
        }
        public class ChestSyncActiveEventArgs
        {
            public ChestSyncActiveEventArgs(BPlayer plr, SyncPlayerChest data)
            {
                Player = plr;
                Data = data;
            }
            public bool Handled { get; set; } = false;
            public BPlayer Player { get; private set; }
            public SyncPlayerChest Data { get; private set; }
            public Point Position => Data.Position.ToPoint();
        }
    }
}
