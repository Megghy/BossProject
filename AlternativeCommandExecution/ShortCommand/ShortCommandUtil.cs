using AlternativeCommandExecution.Extensions;
using TShockAPI;

namespace AlternativeCommandExecution.ShortCommand
{
    public static class ShortCommandUtil
    {
        private static readonly Type CommandsType = typeof(Commands);
        public static bool RunCmd(string cmdName, string cmdPrefix, TSPlayer player, string[] args)
        {
            var sc = Plugin.ShortCommands.Where(x => x.HasName(cmdName)).ToList();
            if (sc.Count != 0)
            {
                foreach (var s in sc)
                {
                    try
                    {
                        foreach (var c in s.Convert(new CommandExectionContext(player), args))
                        {
                            BossFramework.BUtils.HandleCommandDirect(player, c, cmdName, args.ToList(), cmdPrefix == TShock.Config.Settings.CommandSilentSpecifier, false);
                        }
                    }
                    catch (LackOfArgumentException ex)
                    {
                        player.SendErrorMessage(ex.Message);
                    }
                }
                return true;
            }
            return false;
        }
    }
}
