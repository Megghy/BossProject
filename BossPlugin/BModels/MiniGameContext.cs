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
    public sealed class MiniGameContext : IDisposable
    {
        /// <summary>
        /// <para>小游戏上下文构造函数</para>
        /// <para>如非必要尽量从 <see cref="BCore.MiniGameManager.CreateGame(IMiniGame, BPlayer, bool)" /> 创建实例</para>
        /// </summary>
        /// <param name="game"></param>
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
        private IMiniGame? _game;
        private Timer _updateTimer { get; set; } = new()
        {
            Interval = 1000 / BCore.MiniGameManager.UPDATE_PRE_SECEND,
            AutoReset = true
        };
        /// <summary>
        /// 游玩的玩家
        /// </summary>
        public List<BPlayer> Players { get; } = new();
        /// <summary>
        /// 表示是否已调用过 <see cref="Init" />
        /// </summary>
        public bool IsInitialized { get; private set; } = false;
        /// <summary>
        /// 进行过的更新次数
        /// </summary>
        public long GameTime { get; private set; } = 0;
        /// <summary>
        /// 游戏提供的游戏名称, 此处返回第一个
        /// </summary>
        public string Name => _game?.Names.First() ?? "Unknown";
        public MiniGameState GameState => _game?.State ?? MiniGameState.End;

        #region 方法
        private void UpdateTimerCallBack(object? sender, ElapsedEventArgs e)
        {
            if (_game?.State == MiniGameState.End)
                Dispose();
            _game?.Update(GameTime);
            GameTime++;
        }
        /// <summary>
        /// 初始化游戏
        /// </summary>
        /// <param name="creator">游戏创建者</param>
        /// <returns></returns>
        public MiniGameContext Init(BPlayer creator)
        {
            if (!IsInitialized)
            {
                _game?.Init(creator);
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
            _game?.Start();
            return this;
        }
        public bool Join(BPlayer plr)
        {
            if (_game?.Join(plr) == true)
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
                _game?.Leave(plr);
                if (plr.PlayingGame == this)
                    plr.PlayingGame = null;
            }
        }
        public override string ToString()
        {
            return $"{Name}:{GID}<{Players?.Count}>";
        }

        public void Dispose()
        {
            Players.ForEach(p =>
            {
                if (p.PlayingGame == this)
                    p.PlayingGame = null;
            });
            _updateTimer.Dispose();
            _game?.Stop();
            _game = null;
            Players.Clear();
            BLog.Info($"小游戏实例 [{this}] 已销毁");
        }
        #endregion
    }
}
