using AlternativeCommandExecution.ShortCommand;
using TerrariaApi.Server;
using TShockAPI;
using TShockAPI.Hooks;

namespace AlternativeCommandExecution
{
    partial class Plugin
    {
        private static void OnServerCommand(CommandEventArgs args)
        {
            if (args.Handled)
                return;

            if (string.IsNullOrWhiteSpace(args.Command))
            {
                args.Handled = true;
                return;
            }

            var commandText = IsValidCmd(args.Command) ? args.Command : Commands.Specifier + args.Command;

            ShortCommandUtil.HandleCommand(TSPlayer.Server, commandText);

            args.Handled = true;
        }

        private static void OnChat(PlayerChatEventArgs args)
        {
            if (args.Handled)
                return;
            if (IsValidCmd(args.RawText))
            {
                args.Handled = ShortCommandUtil.HandleCommand(args.Player, args.RawText);
            }
        }

        private static void LoadShortCommands()
        {
            var list = new List<ShortCommand.ShortCommand>();

            foreach (var item in Config.ShortCommands)
            {
                try
                {
                    list.Add(ShortCommand.ShortCommand.Create(item.ParameterDescription, item.CommandLines, item.Names));
                }
                catch (CommandParseException ex)
                {
                    Console.WriteLine("加载简写指令时读取失败：{0}", ex);
                }
            }

            ShortCommands = list.ToArray();
        }

        public static ShortCommand.ShortCommand[] ShortCommands { get; private set; }

        private static bool IsValidCmd(string commandText)
        {
            return (
                       commandText.StartsWith(TShock.Config.Settings.CommandSpecifier) ||
                       commandText.StartsWith(TShock.Config.Settings.CommandSilentSpecifier) ||
                       commandText.StartsWith(Config.CommandSpecifier) ||
                       commandText.StartsWith(Config.CommandSpecifier2)
                   )
                   && !string.IsNullOrWhiteSpace(commandText.Substring(1));
        }
    }
}
