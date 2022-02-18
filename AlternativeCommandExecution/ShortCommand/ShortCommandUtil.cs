using AlternativeCommandExecution.Extensions;
using TShockAPI;

namespace AlternativeCommandExecution.ShortCommand
{
    public static class ShortCommandUtil
    {
        private static readonly Type CommandsType = typeof(Commands);

        public static bool HandleCommand(TSPlayer player, string text)
        {
            if (!Internal_ParseCmd(text, out var cmdText, out var cmdName, out var args, out var silent))
            {
                player.SendErrorMessage("指令无效；键入 {0}help 以获取可用指令。", Commands.Specifier);
                return true;
            }

            var sc = Plugin.ShortCommands.Where(x => x.HasName(cmdName)).ToList();

            if (sc.Count != 0)
            {
                var cmdPrefix = silent ? Commands.SilentSpecifier : Commands.Specifier;

                foreach (var s in sc)
                {
                    try
                    {
                        foreach (var c in s.Convert(new CommandExectionContext(player), args.ToArray()))
                        {
                            Commands.HandleCommand(player, cmdPrefix + c);
                        }
                    }
                    catch (LackOfArgumentException ex)
                    {
                        player.SendErrorMessage(ex.Message);
                    }
                }
                return true;
            }
            else
            {
                return Commands.HandleCommand(player, text);
            }
        }

        public static bool HandleCommandIgnorePermission(TSPlayer player, string text)
        {
            if (!Internal_ParseCmd(text, out var cmdText, out var cmdName, out var args, out var silent))
            {
                player.SendErrorMessage("指令无效；键入 {0}help 以获取可用指令。", Commands.Specifier);
                return false;
            }

            var sc = Plugin.ShortCommands.Where(x => x.HasName(cmdName)).ToList();

            if (sc.Count == 0)
            {
                return Internal_HandleCommandIgnorePermission(player, cmdText, cmdName, args, silent);
            }

            var cmdPrefix = silent ? Commands.SilentSpecifier : Commands.Specifier;

            foreach (var s in sc)
            {
                try
                {
                    foreach (var c in s.Convert(new CommandExectionContext(player), args.ToArray()))
                    {
                        if (!Internal_ParseCmd(cmdPrefix + c, out var ct, out var cn, out var ar, out var si))
                        {
                            continue;
                        }
                        Internal_HandleCommandIgnorePermission(player, ct, cn, ar, si);
                    }
                }
                catch (LackOfArgumentException ex)
                {
                    player.SendErrorMessage(ex.Message);
                }
            }

            return true;
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
                CommandsType.CallPrivateStaticMethod<List<string>>("ParseParameters", cmdText.Substring(index));
            return true;
        }

        private static bool Internal_HandleCommandIgnorePermission(TSPlayer player, string cmdText, string cmdName, List<string> args, bool silent)
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
                if (cmd.CanRun(player) || cmd.Permissions.Any(Plugin.Config.SkipablePermissions.Contains))
                {
                    cmd.CommandDelegate?.Invoke(new CommandArgs(cmdText, silent, player, args));
                }
                else
                {
                    player.SendErrorMessage("你没有权限执行该指令。", Commands.Specifier);
                }
            }
            return true;
        }
    }
}
