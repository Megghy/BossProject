using AlternativeCommandExecution.Extensions;
using TShockAPI;

namespace AlternativeCommandExecution.ShortCommand
{
    public static class ShortCommandUtil
    {
        private static readonly Type CommandsType = typeof(Commands);

        public static bool HandleCommand(TSPlayer player, string text)
        {
            if (Internal_ParseCmd(text, out var cmdText, out var cmdName, out var args, out var silent))
            {
                var oldTempGroup = player.tempGroup;
                player.tempGroup = SuperAdminGroup.Default;
                if (!RunCmd(cmdName, silent ? Commands.SilentSpecifier : Commands.Specifier, player, args.ToArray()))
                    Commands.HandleCommand(player, text);
                player.tempGroup = oldTempGroup;
                return true;
            }
            else
                player.SendErrorMessage("键入的指令无效；使用 {0}help 查看有效指令。", Commands.Specifier);
            return false;
        }
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
                            Internal_HandleCommand(player, c, cmdName, args.ToList(), cmdPrefix == TShock.Config.Settings.CommandSilentSpecifier);
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
        private static bool Internal_ParseCmd(string text, out string cmdText, out string cmdName, out List<string> args, out bool silent)
        {
            cmdText = text.Remove(0, 1);
            var cmdPrefix = text[0].ToString();
            silent = cmdPrefix == Commands.SilentSpecifier;

            var index = -1;
            for (var i = 0; i < cmdText.Length; i++)
            {
                if (CommandsType.CallPrivateStaticMethod<bool>("IsWhiteSpace", cmdText[i]))
                {
                    index = i;
                    break;
                }
            }
            if (index == 0) // Space after the command specifier should not be supported
            {
                args = null;
                cmdName = null;
                return false;
            }
            cmdName = index < 0 ? cmdText.ToLower() : cmdText.Substring(0, index).ToLower();

            args = index < 0 ?
                new List<string>() :
                Commands.ParseParameters(cmdText.Substring(index));
            return true;
        }
        private static bool Internal_HandleCommand(TSPlayer player, string cmdText, string cmdName, List<string> args, bool silent)
        {
            var cmds = Commands.ChatCommands.FindAll(x => x.HasAlias(cmdName));

            if (cmds.Count == 0)
            {
                if (player.AwaitingResponse.ContainsKey(cmdName))
                {
                    Action<CommandArgs> call = player.AwaitingResponse[cmdName];
                    player.AwaitingResponse.Remove(cmdName);
                    call(new CommandArgs(cmdText, player, args));
                    return true;
                }
                player.SendErrorMessage("键入的指令无效；使用 {0}help 查看有效指令。", Commands.Specifier);
                return true;
            }
            foreach (var cmd in cmds)
            {
                cmd.CommandDelegate?.Invoke(new CommandArgs(cmdText, silent, player, args));
            }
            return true;
        }
    }
}
