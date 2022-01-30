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
            Player = plr;
        }
        public override void Init()
        {
            Player = TShock.Players.FirstOrDefault(p => p?.Account?.ID.ToString() == ID);
        }

        #region 变量
        public TSPlayer Player { get; internal set; }
        public string Name => Player?.Name ?? "unknown";

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
            Player?.SendRawData(p.Serialize());
        }
        public void SendCombatMessage(string msg, Color color = default, bool randomPosition = true)
        {
            if (Player != null)
            {
                color = color == default ? Color.White : color;
                Random random = new();
                Player.SendData(PacketTypes.CreateCombatTextExtended, msg, (int)color.PackedValue, Player.X + (randomPosition ? random.Next(-75, 75) : 0), Player.Y + (randomPosition ? random.Next(-50, 50) : 0));
            }
        }
        public void SendCombatMessage(string msg, Point p, Color color = default)
        {
            if (Player != null)
            {
                color = color == default ? Color.White : color;
                Player.SendData(PacketTypes.CreateCombatTextExtended, msg, (int)color.PackedValue, p.X, p.Y);
            }
        }
        public void SendSuccessEX(object text)
        {
            Player.SendEX(text, new Color(120, 194, 96));
        }

        public void SendInfoEX(object text)
        {
            Player.SendEX(text, new Color(216, 212, 82));
        }
        public void SendErrorEX(object text)
        {
            Player.SendEX(text, new Color(195, 83, 83));
        }
        public void SendEX(object text, Color color = default)
        {
            Player.SendEX(text, color);
        }
        #endregion
    }
}
