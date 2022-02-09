using BossFramework.BAttributes;
using BossFramework.BInterfaces;
using BossFramework.BModels;
using System;
using System.Collections.Generic;
using System.Linq;

namespace BossFramework.BCore
{
    public static class MiniGameManager
    {
        /// <summary>
        /// 每秒更新次数
        /// </summary>
        public const int UPDATE_PRE_SECEND = 10;
        /// <summary>
        /// 储存所有小游戏初始实例
        /// 别直接用这里头的, 先克隆一个
        /// </summary>
        public static IMiniGame[] Games { get; private set; } = Array.Empty<IMiniGame>();
        /// <summary>
        /// 服务器内正在运行的小游戏
        /// </summary>
        public static List<MiniGameContext> RunningGames { get; set; } = new();

        [AutoInit("加载小游戏")]
        [Reloadable]
        private static void LoadAllMiniGames()
        {
            Games = ScriptManager.LoadScripts<IMiniGame>(ScriptManager.MiniGameScriptPath);
            BLog.Success($"成功加载 {Games.Length} 个小游戏");
        }
        public static bool TryFindGamesByName(string name, out IMiniGame[] games)
        {
            games = Games.Where(g => g.Names.Any(n => n.IsSimilarWith(name))).ToArray();
            return games.Any();
        }
        public static bool TryFindSingleGameByName(string name, out IMiniGame game)
        {
            if (TryFindGamesByName(name, out var games))
            {
                game = games.FirstOrDefault();
                return true;
            }
            else
            {
                game = null;
                return false;
            }
        }
        /// <summary>
        /// 声明一个新小游戏实例
        /// </summary>
        /// <param name="game"></param>
        /// <returns></returns>
        public static IMiniGame NewGameInstance(this IMiniGame game) => (IMiniGame)Activator.CreateInstance(game.GetType())!;
        /// <summary>
        /// 创建一个小游戏
        /// </summary>
        /// <param name="game"></param>
        /// <param name="creator"></param>
        /// <param name="init"></param>
        /// <returns>当达到最大数量时返回null</returns>
        public static MiniGameContext CreateGame(IMiniGame game, BPlayer creator, bool init = true)
        {
            if (RunningGames.Where(g => g.Name == game.Names.First()).Count() < game.MaxCount)
            {
                var context = new MiniGameContext(game.NewGameInstance()); //创建新实例
                if (init)
                    context.Init(creator);
                RunningGames.Add(context);
                BLog.DEBUG($"创建新小游戏实例 [{context}]");
                return context;
            }
            else
            {
                BLog.Warn($"小游戏 [{game.Names.First()}] 达到最大数量限制 [{game.MaxCount}], 无法继续创建");
                return null;
            }
        }

        #region 玩家类拓展函数
        /// <summary>
        /// 玩家是否在游戏中
        /// </summary>
        /// <param name="plr"></param>
        /// <param name="gameName">可选 游戏名</param>
        /// <returns></returns>
        public static bool IsInGame(this BPlayer plr, string gameName = null)
        {
            if (string.IsNullOrEmpty(gameName))
                return plr.PlayingGame != null;
            else
                return plr.PlayingGame?.Name == gameName;
        }

        /// <summary>
        /// 尝试加入一场游戏
        /// </summary>
        public static bool JoinGame(this BPlayer plr, MiniGameContext game)
        {
            if (!plr.IsInGame())
            {
                if (game.Join(plr))
                {
                    plr.PlayingGame = game;
                    BLog.DEBUG($"[{plr}] 加入小游戏 <{game}>");
                    return true;
                }
                else
                    return false;
            }
            else
            {
                BLog.DEBUG($"[{plr}] 尝试加入 <{game}> 失败: 已处于一场游戏中");
                return false;
            }
        }
        #endregion
    }
}
