using BossFramework.BModels;

namespace BossFramework.BInterfaces
{
    public abstract class BaseMiniGame
    {
        public static int UPDATE_PER_SECOND
            => BCore.MiniGameManager.UPDATE_PRE_SECEND;
        public static string MINIGAME_GUI_TOUCH_PERMISSION
            => BCore.MiniGameManager.MINIGAME_GUI_TOUCH_PERMISSION;
        public BaseMiniGame(Guid id)
        {
            GId = id;
        }
        #region 成员
        public Guid GId { get; set; }
        /// <summary>
        /// 同时允许允许的小游戏数量
        /// </summary>
        public virtual int MaxCount { get; } = 1;
        /// <summary>
        /// 小游戏名称
        /// </summary>
        public abstract string[] Names { get; }
        /// <summary>
        /// 作者
        /// </summary>
        public abstract string Author { get; }
        /// <summary>
        /// 描述
        /// </summary>
        public abstract string Description { get; }
        /// <summary>
        /// 状态
        /// </summary>
        public MiniGameState State { get; protected set; } = MiniGameState.Waiting;


        /// <summary>
        /// 游玩的玩家
        /// </summary>
        public List<BPlayer> Players { get; } = new();
        #endregion

        #region 方法
        /// <summary>
        /// 初始化小游戏
        /// </summary>
        /// <param name="creator">创建者</param>
        public abstract void Init(BPlayer creator);
        /// <summary>
        /// 开始
        /// </summary>
        public abstract void Start();
        /// <summary>
        /// 游戏逻辑更新, 每秒10次
        /// </summary>
        /// /// <param name="gameTime">次数</param>
        public abstract void Update(long gameTime);
        /// <summary>
        /// 玩家加入游戏
        /// </summary>
        /// <param name="player"></param>
        public abstract bool Join(BPlayer plr);
        /// <summary>
        /// 玩家离开游戏
        /// </summary>
        /// <param name="player"></param>
        /// <returns>是否成功加入</returns>
        public virtual void Leave(BPlayer plr)
        {

        }
        /// <summary>
        /// 尝试暂停
        /// </summary>
        public virtual void Pause()
        {

        }
        /// <summary>
        /// 停止
        /// </summary>
        public abstract void Stop();
        public abstract void Dispose();
        #endregion
    }
}
