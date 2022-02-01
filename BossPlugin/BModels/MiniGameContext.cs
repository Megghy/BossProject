using BossPlugin.BInterfaces;
using System;
using System.Collections.Generic;
using System.Linq;

namespace BossPlugin.BModels
{
    /// <summary> 
    /// 小游戏的运行时容器
    /// </summary>
    public class MiniGameContext
    {
        public MiniGameContext(IMiniGame game)
        {
            Game = game;
        }
        /// <summary>
        /// 小游戏唯一标识符
        /// </summary>
        public Guid GID { get; } = Guid.NewGuid();
        /// <summary>
        /// 游戏实例
        /// </summary>
        public IMiniGame Game { get; private set; }
        /// <summary>
        /// 游玩的玩家
        /// </summary>
        public List<BPlayer> Players { get; } = new();

        public string Name => Game.Names.First();

        #region 方法
        public bool Join(BPlayer plr)
        {
            if (Game.Join(plr))
            {
                Players.Add(plr);
                plr.PlayingGame = this;
                return true;
            }
            else
                return false;
        }
        public void Leave(BPlayer plr)
        {
            if (Players.Contains(plr))
            {
                Players.Remove(plr);
                plr.PlayingGame = null;
                Game.Leave(plr);
            }
        }
        public override string ToString()
        {
            return $"{Name}:{GID}<{Players?.Count}>";
        }
        #endregion
    }
}
