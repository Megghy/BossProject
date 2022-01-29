using BossPlugin.BInterfaces;
using System;

namespace BossPlugin.BCore
{
    /// <summary> 
    /// 小游戏的运行时容器
    /// </summary>
    public class MiniGameContainer
    {
        public MiniGameContainer(IMiniGame game)
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

        public string Name => Game.Name;


        #region 方法
        public bool Join(BPlayer player) => Game.Join(player);
        public override string ToString()
        {
            return $"{Name}:{GID}<{Game.Players.Count}>";
        }
        #endregion
    }
}
