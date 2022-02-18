using BossFramework.BAttributes;
using BossFramework.BInterfaces;
using BossFramework.BModels;
using System.Linq;

namespace BossFramework.BCore.Cmds
{
    public class BossCmd : BaseCommand
    {
        public override string[] Names { get; } = new[] { "boss" };

        [SubCommand("repl", Permission = "boss.admin.repl")]
        public static void REPL(SubCommandArgs args)
        {
            var code = args.FullCommand.Remove(0, args.SubCommandName.Length + args.CommandName.Length + 2);
        }
        [SubCommand("ncw", Permission = "boss.admin.changecustomweaponmode")]
        public static void ncw(SubCommandArgs args)
        {
            if(args.Any())
            {
                if(BInfo.OnlinePlayers.FirstOrDefault(p => p.Name.IsSimilarWith(args[0])) is { } plr)
                {
                    bool enable = args.Count() > 1 
                        ? args[1].ToLower() is "on" or "true" or "enable"
                        : !plr.IsCustomWeaponMode;
                    plr.ChangeCustomWeaponMode(enable);
                }
                else
                    args.BPlayer.SendErrorMsg($"未找到名为 {args[0]} 的玩家");
            }
            else
                args.BPlayer.SendErrorMsg($"格式错误. /boss ncw <玩家名> (on/off)");
        }
    }
}
