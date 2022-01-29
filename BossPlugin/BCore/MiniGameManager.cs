using BossPlugin.BAttributes;
using BossPlugin.BInterfaces;
using BossPlugin.BModels;
using System;
using System.Collections.Generic;
using System.Linq;

namespace BossPlugin.BCore
{
    public static class MiniGameManager
    {
        /// <summary>
        /// 储存所有小游戏初始实例
        /// 别直接用这里头的, 先克隆一个
        /// </summary>
        public static IMiniGame[] Games { get; private set; } = Array.Empty<IMiniGame>();
        /// <summary>
        /// 服务器内正在运行的小游戏
        /// </summary>
        public static List<MiniGameContainer> RunningGames { get; set; } = new();

        [AutoInit("加载小游戏")]
        [Reloadable]
        public static void LoadAllMiniGames()
        {
            Games = ScriptManager.LoadScripts<IMiniGame>(ScriptManager.MiniGameScriptPath);
            BLog.Success($"成功加载 {Games.Length} 个小游戏");
        }

        /// <summary>
        /// 通过名字寻找小游戏
        /// </summary>
        /// <param name="name"></param>
        /// <returns>没有则为null</returns>
        public static IMiniGame TryFindByName(string name)
        {
            return Games.FirstOrDefault(g => g.Name.ToLower() == name.ToLower() || g.Name.StartsWith(name));
        }
        public static MiniGameContainer CreateGame(IMiniGame game)
        {
            if (RunningGames.Where(g => g.Name == game.Name).Count() < game.MaxCount)
            {
                var g = new MiniGameContainer(game);
                RunningGames.Add(g);
                BLog.Info($"创建新小游戏实例: {g}");
                return g;
            }
            else
            {
                BLog.Warn($"小游戏 [{game.Name}] 达到最大数量限制 [{game.MaxCount}], 无法继续创建");
                return null;
            }
        }
    }
}
