using BossPlugin.BInterfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Timers;

namespace BossPlugin.BModels
{
    /// <summary> 
    /// 小游戏的运行时容器
    /// </summary>
    public class MiniGameContext : IDisposable
    {
        public MiniGameContext(IMiniGame game)
        {
            _game = game;
            _updateTimer.Elapsed += UpdateTimerCallBack;
        }
        /// <summary>
        /// 小游戏唯一标识符
        /// </summary>
        public Guid GID { get; } = Guid.NewGuid();
        /// <summary>
        /// 游戏实例
        /// </summary>
        private IMiniGame _game;
        private Timer _updateTimer { get; set; } = new()
        {
            Interval = 1000 / BCore.MiniGameManager.UPDATE_PRE_SECEND,
            AutoReset = true
        };
        /// <summary>
        /// 游玩的玩家
        /// </summary>
        public List<BPlayer> Players { get; } = new();

        public bool IsInitialized { get; private set; } = false;

        public long GameTime { get; private set; } = 0;

        public string Name => _game.Names.First();

        #region 方法
        private void UpdateTimerCallBack(object sender, ElapsedEventArgs e)
        {
            if (_game.State == MiniGameState.End)
                Dispose();
            _game.Update(GameTime);
            GameTime++;
        }
        public MiniGameContext Init(BPlayer creator = null)
        {
            if (!IsInitialized)
            {
                _game.Init(creator);
                _updateTimer.Start();
                IsInitialized = true;
            }
            return this;
        }
        /// <summary>
        /// 开始游戏
        /// </summary>
        public MiniGameContext Start()
        {
            _game.Start();
            return this;
        }
        public bool Join(BPlayer plr)
        {
            if (_game.Join(plr))
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
                _game.Leave(plr);
            }
        }
        public override string ToString()
        {
            return $"{Name}:{GID}<{Players?.Count}>";
        }

        public void Dispose()
        {
            _game?.Stop();
            _updateTimer.Stop();
            Players.Clear();
            BLog.Info($"小游戏实例 [{this}] 已销毁");
        }
        #endregion
    }
}
