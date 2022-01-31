using BossPlugin.DB;
using Microsoft.Xna.Framework;
using System;
using System.Linq;
using TrProtocol;
using TShockAPI;

namespace BossPlugin.BModels
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

        #region 小游戏部分
        public long Point { get; set; }
        /// <summary>
        /// 正在玩的游戏, 通过 JoinGame 加入
        /// </summary>
        public MiniGameContext PlayingGame { get; internal set; }
        #endregion

        #endregion

        #region 常用方法
        public override string ToString() => $"{Name}:{ID}";
        /// <summary>
        /// 向玩家发送数据包
        /// </summary>
        /// <param name="p"></param>
        public void SendPacket(Packet p)
        {
            TsPlayer?.SendRawData(p.Serialize());
        }
        public void SendCombatMessage(string msg, Color color = default, bool randomPosition = true)
        {
            if (TsPlayer != null)
            {
                color = color == default ? Color.White : color;
                Random random = new();
                TsPlayer.SendData(PacketTypes.CreateCombatTextExtended, msg, (int)color.PackedValue, TsPlayer.X + (randomPosition ? random.Next(-75, 75) : 0), TsPlayer.Y + (randomPosition ? random.Next(-50, 50) : 0));
            }
        }
        public void SendCombatMessage(string msg, Point p, Color color = default)
        {
            if (TsPlayer != null)
            {
                color = color == default ? Color.White : color;
                TsPlayer.SendData(PacketTypes.CreateCombatTextExtended, msg, (int)color.PackedValue, p.X, p.Y);
            }
        }
        public void SendSuccessEX(object text)
        {
            TsPlayer.SendEX(text, new Color(120, 194, 96));
        }

        public void SendInfoEX(object text)
        {
            TsPlayer.SendEX(text, new Color(216, 212, 82));
        }
        public void SendErrorEX(object text)
        {
            TsPlayer.SendEX(text, new Color(195, 83, 83));
        }
        public void SendEX(object text, Color color = default)
        {
            TsPlayer.SendEX(text, color);
        }
        #endregion
    }
}
