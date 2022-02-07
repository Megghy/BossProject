using BossFramework.BAttributes;
using BossFramework.BInterfaces;
using BossFramework.BModels;

namespace BossFramework.BCore.Cmds
{
    public class BossCmd : BaseCommand
    {
        public override string[] Names { get; } = new[] { "boss" };

        [SubCommand("eval", Permission = "boss.admin.eval")]
        public static void Eval(SubCommandArgs args)
        {
            var code = args.FullCommand.Remove(0, args.SubCommandName.Length + args.CommandName.Length + 2);
        }
    }
}
