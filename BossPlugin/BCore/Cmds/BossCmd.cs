using BossPlugin.BAttributes;
using BossPlugin.BInterfaces;
using BossPlugin.BModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BossPlugin.BCore.Cmds
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
