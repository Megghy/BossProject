using BossPlugin.BAttributes;
using BossPlugin.BInterfaces;
using BossPlugin.BModels;
using System.Linq;

namespace BossPlugin.BCore.Cmds
{
    public class MiniGameCmd : BaseCommand
    {
        public override string[] Names { get; } = new[] { "mini", "minigame", "mg" };

        #region 玩家命令
        /// <summary>
        /// 
        /// </summary>
        /// <param name="args"></param>
        [SubCommand("join", "j", Permission = "boss.minigame.use")]
        public void JoinGame(SubCommandArgs args)
        {
            if (args.Any())
            {

            }
            if (args.BPlayer.IsInGame())
            {

            }
        }
        #endregion

        #region 管理员命令

        #endregion
    }
}
