using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BossPlugin.BModels
{
    public enum MiniGameState
    {
        /// <summary>
        /// 正在等待玩家
        /// </summary>
        Waiting,

        /// <summary>
        /// 游玩中
        /// </summary>
        Playing,

        /// <summary>
        /// 暂停中
        /// </summary>
        Pause,

        /// <summary>
        /// 已结束
        /// </summary>
        End
    }
}
