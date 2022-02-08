using BossFramework.DB;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using TrProtocol;
using TrProtocol.Packets;
using TShockAPI;

namespace BossFramework.BModels
{
    public partial class BPlayer : UserConfigBase<BPlayer>
    {
        public BPlayer() { }
        public BPlayer(TSPlayer plr)
        {
            TsPlayer = plr;
        }
        public override void Init()
        {
            TsPlayer = TShock.Players.FirstOrDefault(p => p?.Account?.ID.ToString() == ID);
        }

        #region 变量
        public TSPlayer TsPlayer { get; internal set; }
        public string Name => TsPlayer?.Name ?? "unknown";
        public byte Index => (byte)(TsPlayer?.Index ?? -1);
        public bool IsRealPlayer => TsPlayer?.RealPlayer ?? false;

        public BRegion CurrentRegion { get; internal set; }

        #region 小游戏部分
        public long Point { get; set; }
        /// <summary>
        /// 正在玩的游戏, 通过 JoinGame 加入
        /// </summary>
        public MiniGameContext PlayingGame { get; internal set; }
        #endregion

        #endregion

        #region 常用方法
        public override string ToString() => $"{Name}";
        /// <summary>
        /// 向玩家发送数据包
        /// </summary>
        /// <param name="p"></param>
        public void SendPacket(Packet p)
        {
            TsPlayer?.SendRawData(p.SerializePacket());
        }
        public void SendCombatMessage(string msg, Color color = default, bool randomPosition = true)
        {
            color = color == default ? Color.White : color;
            Random random = new();
            TsPlayer!.SendData(PacketTypes.CreateCombatTextExtended, msg, (int)color.PackedValue, TsPlayer.X + (randomPosition ? random.Next(-75, 75) : 0), TsPlayer.Y + (randomPosition ? random.Next(-50, 50) : 0));
        }
        public void SendCombatMessage(string msg, Point p, Color color = default)
        {
            color = color == default ? Color.White : color;
            TsPlayer!.SendData(PacketTypes.CreateCombatTextExtended, msg, (int)color.PackedValue, p.X, p.Y);
        }
        public void SendSuccessEX(object text)
        {
            TsPlayer?.SendEX(text, new Color(120, 194, 96));
        }

        public void SendInfoEX(object text)
        {
            TsPlayer?.SendEX(text, new Color(216, 212, 82));
        }
        public void SendErrorEX(object text)
        {
            TsPlayer?.SendEX(text, new Color(195, 83, 83));
        }
        public void SendEX(object text, Color color = default)
        {
            TsPlayer?.SendEX(text, color);
        }
        public void SendMultipleMatchError(IEnumerable<object> matches)
        {
            TsPlayer?.SendErrorMessage("More than one match found -- unable to decide which is correct: ");

            var lines = PaginationTools.BuildLinesFromTerms(matches.ToArray());
            lines.ForEach(TsPlayer!.SendInfoMessage);

            TsPlayer?.SendErrorMessage("Use \"my query\" for items with spaces.");
            TsPlayer?.SendErrorMessage("Use tsi:[number] or tsn:[username] to distinguish between user IDs and usernames.");
        }

        #region 玩家状态
        /// <summary>
        /// 增加或减少玩家魔法值, 如果不足的话返回 false
        /// </summary>
        /// <param name="value">减少为 - </param>
        public bool ChangeMana(int value)
        {
            if (IsRealPlayer)
                return false;
            if (TsPlayer.TPlayer.statMana + value < 0)
                return false;
            TsPlayer.TPlayer.statMana += value;
            SendPacket(new PlayerMana()
            {
                PlayerSlot = Index,
                StatMana = (short)TsPlayer.TPlayer.statMana,
                StatManaMax = (short)TsPlayer.TPlayer.statLifeMax2
            });
            this.SendPacketToAll(new ManaEffect()
            {
                PlayerSlot = Index,
                Amount = (short)(value < 0 ? -value : value)
            });
            return true;
        }
        #endregion
        #endregion
    }
}
