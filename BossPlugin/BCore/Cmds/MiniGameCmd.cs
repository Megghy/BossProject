using BossPlugin.BAttributes;
using BossPlugin.BInterfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BossPlugin.BCore.Cmds
{
    public class MiniGameCmd : BaseCommand
    {
        public override string[] Names { get; } = new[] { "mini", "minigame", "mg" };

        [SubCommand("join", "j", Permission = "boss.minigame.use")]
        public void JoinGame()
        {

        }
    }
}
