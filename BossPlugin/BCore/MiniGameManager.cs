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
        /// 每秒更新次数
        /// </summary>
        public static readonly int UPDATE_PRE_SECEND = 10; //netfx的css好像用常量有点问题
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
        public static void LoadAllMiniGames()
        {
            Games = ScriptManager.LoadScripts<IMiniGame>(ScriptManager.MiniGameScriptPath);
            BLog.Success($"成功加载 {Games.Length} 个小游戏");
        }
        /// <summary>
        /// 创建一个新小游戏实例
        /// </summary>
        /// <param name="game"></param>
        /// <returns></returns>
        public static IMiniGame CreateGameInstance(this IMiniGame game) => (IMiniGame)Activator.CreateInstance(game.GetType());
        /// <summary>
        /// 通过名字寻找小游戏
        /// </summary>
        /// <param name="name"></param>
        /// <returns>没有则为null</returns>
        public static IMiniGame TryFindByName(string name)
        {
            return Games.FirstOrDefault(g => g.Names.Any(n =>n .ToLower() == name.ToLower() || n.StartsWith(name)));
        }
        public static MiniGameContext CreateGame(IMiniGame game, BPlayer creator = null)
        {
            if (RunningGames.Where(g => g.Name == game.Names.First()).Count() < game.MaxCount)
            {
                var context = new MiniGameContext(game.CreateGameInstance()); //创建新实例
                context.Game.Init(creator);
                RunningGames.Add(context);
                BLog.Info($"创建新小游戏实例: {context}");
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
        /// <param name="name">可选 游戏名</param>
        /// <returns></returns>
        public static bool IsInGame(this BPlayer plr, string name = null)
        {
            if (string.IsNullOrEmpty(name))
                return plr.PlayingGame != null;
            else
                return plr.PlayingGame?.Name == name;
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
                    BLog.Success($"[{plr}] 加入小游戏 <{game}>");
                    return true;
                }
                else
                    return false;
            }
            else
            {
                plr.Player.SendInfoMessage($"你已处于一局游戏中");
                BLog.Log($"[{plr}] 尝试加入 <{game}> 失败: 已处于一场游戏中");
                return false;
            }
        }
        #endregion
    }
}
