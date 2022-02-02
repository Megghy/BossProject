using BossPlugin.BAttributes;
using BossPlugin.BInterfaces;
using BossPlugin.BModels;
using System.Linq;

namespace BossPlugin.BCore.Cmds
{
    public class MiniGameCmd : BaseCommand
    {
        public static readonly string AlreadyInGame_Exception = "你已在一场游戏中";
        public override string[] Names { get; } = new[] { "mini", "minigame", "mg", "game" };

        #region 玩家命令
        /// <summary>
        /// 
        /// </summary>
        /// <param name="args"></param>
        [SubCommand("join", "j", Permission = "boss.minigame.player.join")]
        public void JoinGame(SubCommandArgs args)
        {
            //todo
            //还没想好怎么快速让玩家选择创建好的游戏
        }
        [SubCommand("create", "c", Permission = "boss.minigame.player.create")]
        public void CreateGame(SubCommandArgs args)
        {
            if (args.BPlayer.IsInGame())
            {
                args.BPlayer.SendErrorEX(AlreadyInGame_Exception);
                return;
            }
            if (args.Any())
            {
                if (MiniGameManager.TryFindGamesByName(args.First(), out var games))
                {
                    if (games.Any())
                    {
                        if (games.Length > 1)
                            args.BPlayer.SendMultipleMatchError(games.Select(g => g.Names.FirstOrDefault()));
                        else if (MiniGameManager.CreateGame(games.First(), args.BPlayer) is { } game)
                        {
                            game.Join(args.BPlayer);
                            args.BPlayer.SendSuccessEX($"成功创建小游戏 [{game.Name}]");
                            BLog.Success($"[{args.BPlayer}] 申请创建小游戏 => [{game}]");
                        }
                        else
                            args.BPlayer.SendErrorEX($"当前无法创建小游戏 [{games.First().Names.First()}]");
                    }
                }
                else
                    args.BPlayer.SendErrorEX($"未找到名称中包含 [{args.First()}] 的小游戏");
            }
            else
                args.BPlayer.SendErrorEX($"{BCommand.InvalidInput} /{args.CommandName} {args.SubCommandName} <要创建的小游戏名称>");
        }
        #endregion

        #region 管理员命令

        #endregion
    }
}
