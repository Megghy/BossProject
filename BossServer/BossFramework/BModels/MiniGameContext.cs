using BossFramework.BInterfaces;
using System.Linq;
using System.Timers;

namespace BossFramework.BModels
{
    /// <summary> 
    /// 小游戏的运行时容器
    /// </summary>
    public sealed class MiniGameContext : IDisposable
    {
        /// <summary>
        /// <para>小游戏上下文构造函数</para>
        /// <para>如非必要尽量从 <see cref="BCore.MiniGameManager.CreateGame(BaseMiniGame, BPlayer, bool)" /> 创建实例</para>
        /// </summary>
        /// <param name="game"></param>
        public MiniGameContext(BaseMiniGame game, Guid contextId = default)
        {
            GId = contextId == default ? Guid.NewGuid() : contextId;
            _game = game;
            _updateTimer.Elapsed += UpdateTimerCallBack;
        }
        /// <summary>
        /// 小游戏唯一标识符
        /// </summary>
        public Guid GId { get; init; }
        /// <summary>
        /// 游戏实例
        /// </summary>
        private BaseMiniGame _game;
        private Timer _updateTimer { get; set; } = new()
        {
            Interval = 1000 / BCore.MiniGameManager.UPDATE_PRE_SECEND,
            AutoReset = true
        };
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
        public string Name => _game?.Names.First() ?? "unknown";
        public MiniGameState GameState => _game?.State ?? MiniGameState.End;

        #region 方法
        private void UpdateTimerCallBack(object sender, ElapsedEventArgs e)
        {
            if (_game?.State == MiniGameState.Disposed)
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
                _game.Players.Add(plr);
                plr.PlayingGame = this;
                return true;
            }
            else
                return false;
        }
        public void Leave(BPlayer plr)
        {
            if (_game.Players.Contains(plr))
            {
                _game.Players.Remove(plr);
                _game?.Leave(plr);
                if (plr.PlayingGame == this)
                    plr.PlayingGame = null;
            }
        }
        public override string ToString()
        {
            return $"{Name}:{GId}<{_game?.Players?.Count}>";
        }

        public void Dispose()
        {
            _game?.Players.ForEach(p =>
            {
                if (p.PlayingGame?.GId == this.GId)
                    p.PlayingGame = null;
            });
            _game?.Players.Clear();
            _game?.Stop();
            _game = null;
            _updateTimer.Dispose();
            BCore.MiniGameManager.RunningGames.Remove(this);
            BLog.DEBUG($"小游戏实例 [{this}] 已销毁");
        }
        #endregion
    }
}
