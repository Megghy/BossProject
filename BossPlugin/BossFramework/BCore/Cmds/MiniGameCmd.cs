using BossFramework.BAttributes;
using BossFramework.BInterfaces;
using BossFramework.BModels;

namespace BossFramework.BCore.Cmds
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
            if (args.Player.IsInGame())
            {
                args.Player.SendErrorMsg(AlreadyInGame_Exception);
                return;
            }
            if (args.Any())
            {
                if (MiniGameManager.TryFindGamesByName(args.First(), out var games))
                {
                    if (games.Any())
                    {
                        if (games.Length > 1)
                            args.Player.SendMultipleMatchError(games.Select(g => g.Names.FirstOrDefault()));
                        else
                        {
                            if (args.Player.PlayingGame != null && !args.TsPlayer.HasPermission("boss.minigame.admin.multigame"))
                                args.SendErrorMsg("你已处于一场游戏中");
                            else
                            {
                                if (MiniGameManager.CreateGame(games.First(), args.Player) is { } game)
                                {
                                    game.Join(args.Player);
                                    args.Player.SendSuccessMsg($"成功创建小游戏 [{game.Name}]");
                                    BLog.Success($"[{args.Player}] 申请创建小游戏 => [{game}]");
                                }
                                else
                                    args.Player.SendErrorMsg($"当前无法创建小游戏 [{games.First().Names.First()}]");
                            }
                        }
                    }
                }
                else
                    args.Player.SendErrorMsg($"未找到名称中包含 [{args.First()}] 的小游戏");
            }
            else
                args.Player.SendErrorMsg($"{BCommand.InvalidInput} /{args.CommandName} {args.SubCommandName} <要创建的小游戏名称>");
        }
        #endregion

        #region 管理员命令
        [SubCommand("delpannel", "dp", "removepanel", Permission = "boss.minigame.admin.delpanel")]
        public void DelGame(SubCommandArgs args)
        {
            args.Player.WantDelGame = !args.Player.WantDelGame;
            if (args.Player.WantDelGame)
                args.SendSuccessMsg($"点击想要删除的小游戏面板");
            else
                args.SendErrorMsg("已取消");
        }
        #endregion
    }
}
