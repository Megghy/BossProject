using BossPlugin.BInterfaces;
using BossPlugin.BModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TShockAPI;

namespace BossPlugin.BModels
{
    public partial class BPlayer
    {
        public BPlayer(TSPlayer plr)
        {
            Player = plr;
        }
        public TSPlayer Player { get; private set; }
        public string Name => Player.Name;
        
    }
    /// <summary>
    /// 小游戏部分   
    /// </summary>
    public partial class BPlayer
    {
        /// <summary>
        /// 正在玩的游戏, 通过 JoinGame 加入
        /// </summary>
        public MiniGameContainer PlayingGame { get; private set; }
        /// <summary>
        /// 尝试加入一场游戏
        /// </summary>
        public bool JoinGame(MiniGameContainer game)
        {
            if (PlayingGame is null)
            {
                if (game.Join(this))
                {
                    PlayingGame = game;
                    BLog.Success($"[{Name}] 加入小游戏 <{game}>");
                    return true;
                }
                else
                    return false;
            }
            else
            {
                Player.SendInfoMessage($"你已处于一局游戏中");
                BLog.Log($"[{Name}] 尝试加入 <{game}> 失败: 已处于一场游戏中");
                return false;
            }
        }
    }
}
