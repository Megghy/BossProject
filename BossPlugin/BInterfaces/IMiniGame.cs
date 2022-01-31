using BossPlugin.BModels;

namespace BossPlugin.BInterfaces
{
    public interface IMiniGame
    {
        #region 成员
        /// <summary>
        /// 同时允许允许的小游戏数量
        /// </summary>
        public int MaxCount { get; }
        /// <summary>
        /// 小游戏名称
        /// </summary>
        public string[] Names { get; }
        /// <summary>
        /// 状态
        /// </summary>
        public MiniGameState State { get; set; }
        #endregion

        #region 方法
        /// <summary>
        /// 初始化小游戏
        /// </summary>
        /// <param name="creator">创建者</param>
        public void Init(BPlayer creator = null);
        /// <summary>
        /// 开始
        /// </summary>
        public void Start();
        /// <summary>
        /// 游戏逻辑更新, 每秒10次
        /// </summary>
        /// /// <param name="gameTime">次数</param>
        public void Update(long gameTime);
        /// <summary>
        /// 玩家加入游戏
        /// </summary>
        /// <param name="player"></param>
        public bool Join(BPlayer plr);
        /// <summary>
        /// 玩家离开游戏
        /// </summary>
        /// <param name="player"></param>
        /// <returns>是否成功加入</returns>
        public void Leave(BPlayer plr);
        /// <summary>
        /// 尝试暂停
        /// </summary>
        public void Pause();
        /// <summary>
        /// 停止
        /// </summary>
        public void Stop();
        #endregion
    }
}
