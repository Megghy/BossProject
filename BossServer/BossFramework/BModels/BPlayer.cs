using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using BossFramework.BCore;
using BossFramework.BInterfaces;
using BossFramework.DB;
using EnchCoreApi.TrProtocol.Interfaces;
using FreeSql.DataAnnotations;
using Microsoft.Xna.Framework;
using Terraria;
using TShockAPI;
using static BossFramework.BModels.BEventArgs;

namespace BossFramework.BModels
{
    public partial class BPlayer : DBStructBase<BPlayer>, ISendMsg
    {
        public static readonly BPlayer Default = new(new(256) { })
        {
            Id = -1
        };
        public BPlayer() { }
        public BPlayer(TSPlayer plr)
        {
            TsPlayer = plr;
            Init();
        }
        public override void Init()
        {
            TsPlayer ??= TShock.Players.FirstOrDefault(p => p?.Account?.ID == Id);

            IsCustomWeaponMode = false;
            IsChangingWeapon = false;
        }

        #region 变量
        public long OnlineTicks { get; set; } = 0;
        public TimeSpan OnlineTime
            => new(OnlineTicks);


        public Stopwatch PingChecker { get; } = new();
        [Column(IsIgnore = true)]
        public int LastPing { get; internal set; } = -1;
        [Column(IsIgnore = true)]
        public long LastPingTime { get; internal set; } = -1;
        [Column(IsIgnore = true)]
        public bool WaitingPing { get; internal set; } = false;
        [Obsolete("Use TSPlayer")]
        public TSPlayer TsPlayer { get; internal set; }
        /// <summary>
        /// tshock的玩家对象
        /// </summary>
        public TSPlayer TSPlayer => TsPlayer;
        [Obsolete("Use TPlayer")]
        public Player TrPlayer => TSPlayer?.TPlayer;
        /// <summary>
        /// tr自身的玩家对象
        /// </summary>
        public Player TRPlayer => TrPlayer;
        /// <summary>
        /// tr自身的玩家对象
        /// </summary>
        public Player TPlayer => TrPlayer;
        public string Name => TSPlayer?.Name ?? "unknown";
        public byte Index => (byte)(TSPlayer?.Index ?? -1);
        public bool IsRealPlayer => TSPlayer?.RealPlayer ?? false;
        public float X => TSPlayer.X;
        public float Y => TSPlayer.Y;
        public int TileX => TSPlayer.TileX;
        public int TileY => TSPlayer.TileY;

        public readonly Dictionary<Func<BaseEventArgs, string>, int> PlayerStatusCallback = new();

        public BaseBWeapon[] Weapons { get; internal set; }
        public BRegion CurrentRegion
            => BCore.BRegionSystem.FindBRegionForRegion(TSPlayer?.CurrentRegion) ?? BRegion.Default;
        public ProjRedirectContext ProjContext => CurrentRegion?.ProjContext;
        /// <summary>
        /// 是否处于使用自定义武器的状态, 修改需使用 <see cref="BCore.BWeaponSystem.ChangeCustomWeaponMode(BPlayer, bool?)"/>
        /// </summary>
        [Column(IsIgnore = true)]
        public bool IsCustomWeaponMode { get; internal set; } = false;
        [Column(IsIgnore = true)]
        public bool IsChangingWeapon { get; internal set; } = false;
        [Column(IsIgnore = true)]
        public NetItem ItemInHand { get; internal set; } = new(0, 0, 0);

        public List<BWeaponRelesedProj> RelesedProjs { get; } = new();



        public (short slot, BSign sign)? WatchingSign { get; internal set; }
        public short LastWatchingSignIndex { get; internal set; } = -1;

        public (short slot, BChest chest)? WatchingChest { get; set; }
        public short LastSyncChestIndex { get; internal set; } = -1;

        #region 小游戏部分
        public long Point { get; set; }
        /// <summary>
        /// 是否将要删除下一个点击的小游戏
        /// </summary>
        [Column(IsIgnore = true)]
        public bool WantDelGame { get; internal set; } = false;
        /// <summary>
        /// 正在玩的游戏, 通过 JoinGame 加入
        /// </summary>
        public MiniGameContext PlayingGame { get; set; }
        #endregion

        #endregion

        #region 常用方法
        public override string ToString() => $"{Name}";
        internal bool CheckSendPacket(IAutoSerializableData p)
            => BNet.PacketHandler.HandleSendData(false, new PacketEventArgs(this, p));
        /// <summary>
        /// 向玩家发送数据包
        /// </summary>
        /// <param name="p"></param>
        public void SendPacket(IAutoSerializableData p)
        {
            if (!CheckSendPacket(p))
                TSPlayer?.SendRawData(p.SerializePacket());
        }
        public void SendPackets<T>(IEnumerable<T> p) where T : IAutoSerializableData
        {
            var packets = new List<IAutoSerializableData>();
            p.ForEach(packet =>
            {
                if (!CheckSendPacket(packet))
                    packets.Add(packet);
            });
            TSPlayer?.SendRawData(packets.GetPacketsByteData());
        }
        public void SendRawData(byte[] data) => TSPlayer?.SendRawData(data);
        public void SendCombatMessage(string msg, Color color = default, bool randomPosition = true)
        {
            color = color == default ? Color.White : color;
            Random random = new();
            TSPlayer!.SendData(PacketTypes.CreateCombatTextExtended, msg, (int)color.PackedValue, TSPlayer.X + (randomPosition ? random.Next(-75, 75) : 0), TSPlayer.Y + (randomPosition ? random.Next(-50, 50) : 0));
        }
        public void SendCombatMessage(string msg, Point p, Color color = default)
        {
            color = color == default ? Color.White : color;
            TSPlayer!.SendData(PacketTypes.CreateCombatTextExtended, msg, (int)color.PackedValue, p.X, p.Y);
        }
        public void SendSuccessMsg(object text)
        {
            TSPlayer?.SendMsg(text, new Color(120, 194, 96));
        }

        public void SendInfoMsg(object text)
        {
            TSPlayer?.SendMsg(text, new Color(216, 212, 82));
        }
        public void SendErrorMsg(object text)
        {
            TSPlayer?.SendMsg(text, new Color(195, 83, 83));
        }
        public void SendMsg(object text, Color color = default)
        {
            TSPlayer?.SendMsg(text, color);
        }
        public void SendMultipleMatchError(IEnumerable<object> matches)
        {
            TSPlayer?.SendErrorMessage("More than one match found -- unable to decide which is correct: ");

            var lines = PaginationTools.BuildLinesFromTerms(matches.ToArray());
            lines.ForEach(TSPlayer!.SendInfoMessage);

            TSPlayer?.SendErrorMessage("Use \"my query\" for items with spaces.");
            TSPlayer?.SendErrorMessage("Use tsi:[number] or tsn:[username] to distinguish between user IDs and usernames.");
        }
        public SyncEquipment RemoveItemPacket(int slot, bool clearServerSideItem = true)
            => new()
            {
                ItemType = 0,
                Prefix = 0,
                Stack = 0,
                ItemSlot = (short)slot,
                PlayerSlot = Index
            };
        public void RemoveItem(int slot, bool clearServerSideItem = true)
        {
            if (clearServerSideItem)
                TRPlayer.inventory[slot]?.SetDefaults();
            SendPacket(RemoveItemPacket(slot));
        }

        public bool GivePoint(int num, string from = "未知")
            => BCore.BPointSystem.ChangePoint(Index, num, from);
        public bool TakePoint(int num, string from = "未知")
            => BCore.BPointSystem.ChangePoint(Index, -num, from);

        public void SetData(string key, object data)
            => TSPlayer?.SetData(key, data);
        public bool ContainsData(string key)
            => TSPlayer?.ContainsData(key) ?? false;
        public T GetData<T>(string key)
            => TSPlayer.GetData<T>(key) ?? default;
        public void RemoveData(string key)
            => TSPlayer?.RemoveData(key);

        #region 玩家状态
        /// <summary>
        /// 增加或减少玩家魔法值, 如果不足的话返回 false
        /// </summary>
        /// <param name="value">减少为 - </param>
        public bool ChangeMana(int value)
        {
            if (IsRealPlayer)
                return false;
            if (TSPlayer.TPlayer.statMana + value < 0)
                return false;
            TSPlayer.TPlayer.statMana += value;
            SendPacket(new PlayerMana()
            {
                PlayerSlot = Index,
                StatMana = (short)TSPlayer.TPlayer.statMana,
                StatManaMax = (short)TSPlayer.TPlayer.statLifeMax2
            });
            BUtils.SendPacketToAll(new ManaEffect()
            {
                PlayerSlot = Index,
                Amount = (short)(value < 0 ? -value : value)
            });
            return true;
        }

        #endregion
        #endregion
    }
    // 一些工具方法, 本来应该放在对应的静态类中用拓展方法实现的, 不过各种脚本引用不方便, 就直接放这个里头了
    public partial class BPlayer
    {
        public bool IsInRegion(string region)
        {
            return BRegionSystem.FindBRegionByName(region) is { } bregion && CurrentRegion == bregion;
        }
    }
}
