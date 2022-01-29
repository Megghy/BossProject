using BossPlugin.BCore;
using BossPlugin.BModels;
using System;
using System.Collections.Generic;

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
        public string Name { get; }
        /// <summary>
        /// 状态
        /// </summary>
        public MiniGameState State { get; set; }
        /// <summary>
        /// 游玩的玩家
        /// </summary>
        public List<BPlayer> Players { get; }
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
        /// 尝试将指定玩家加入游戏
        /// </summary>
        /// <param name="player"></param>
        /// <returns>是否成功加入</returns>
        public bool Join(BPlayer player);
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
