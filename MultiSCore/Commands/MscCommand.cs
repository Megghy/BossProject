using System.Text;
using MultiSCore.Common;
using MultiSCore.Model;
using MultiSCore.Services;
using TShockAPI;

namespace MultiSCore.Commands
{
    public class MscCommand
    {
        private readonly Config _config;
        private readonly SessionManager _sessionManager;
        private readonly Func<string, string> _getText; // 用于本地化的委托

        public MscCommand(Config config, SessionManager sessionManager, Func<string, string> getText)
        {
            _sessionManager = sessionManager;
            _config = config;
            _getText = getText;
        }

        public void Register(Command command)
        {
            command.CommandDelegate = OnCommand;
        }

        public void OnCommand(CommandArgs args)
        {
            var player = args.Player;
            var parameters = args.Parameters;

            var forwardedPlayerInfo = _sessionManager.GetForwardedPlayerInfo(player.Index);
            if (forwardedPlayerInfo != null)
            {
                // 如果玩家是从其他服务器代理过来的，目前不处理，未来可以实现将命令转发回源服务器
                player.SendErrorMsg("此命令不能在子服务器上执行。");
                return;
            }

            if (parameters.Count == 0)
            {
                SendHelpText(player);
                return;
            }

            var subCommand = parameters[0].ToLower();

            switch (subCommand)
            {
                case "switch":
                case "sw":
                    // 确保玩家当前不在跨服状态
                    if (_sessionManager.GetSession(player.Index)?.State != SessionState.Disconnected)
                    {
                        player.SendErrorMsg("你已经在一个子服务器中或正在切换中。");
                        return;
                    }
                    if (parameters.Count < 2)
                    {
                        player.SendErrorMsg($"{_getText("Prompt_InvalidFormat")}\n{_getText("Help_Tp")}");
                        return;
                    }
                    var serverName = parameters[1];
                    var serverInfo = _config.Servers.FirstOrDefault(s => s.Name.Equals(serverName, StringComparison.OrdinalIgnoreCase));

                    if (serverInfo == null)
                    {
                        player.SendErrorMsg(string.Format(_getText("Command_ServerNotFound"), serverName));
                        return;
                    }

                    if (!string.IsNullOrEmpty(serverInfo.Permission) && !player.HasPermission(serverInfo.Permission))
                    {
                        player.SendErrorMsg(string.Format(_getText("Command_NoPermission"), serverInfo.Name));
                        return;
                    }

                    player.SendInfoMsg($"正在切换到服务器: {serverInfo.Name}");
                    TShock.Log.ConsoleInfo(string.Format(_getText("Log_Switch"), player.Name, serverInfo.Name));

                    var sessionForSwitch = _sessionManager.CreateSession(player);
                    sessionForSwitch.SwitchServerAsync(serverInfo).ContinueWith(t =>
                    {
                        if (t.IsFaulted)
                        {
                            player.SendErrorMsg($"切换到 {serverInfo.Name} 失败。请查看服务器日志。");
                            _sessionManager.RemoveSession(player.Index);
                        }
                    });
                    break;
                case "back":
                case "b":
                    var sessionForBack = _sessionManager.GetSession(player.Index);
                    if (sessionForBack != null && sessionForBack.State == SessionState.Connected)
                    {
                        player.SendInfoMsg("正在从子服务器返回...");
                        sessionForBack.ReturnToHost();
                    }
                    else
                    {
                        player.SendErrorMsg(_getText("Command_NotJoined"));
                    }
                    break;
                case "list":
                    player.SendInfoMsg("可用服务器列表:");
                    if (_config.Servers.Count == 0)
                    {
                        player.SendInfoMsg("  (没有配置任何服务器)");
                    }
                    else
                    {
                        foreach (var server in _config.Servers)
                        {
                            var sessionForList = _sessionManager.GetSession(player.Index);
                            var status = (sessionForList != null && sessionForList.TargetServer?.Name == server.Name && sessionForList.State == SessionState.Connected) ? " (当前)" : "";
                            player.SendInfoMsg($"  - {server.Name}{status}");
                        }
                    }
                    break;
                case "debug":
                    HandleDebugCommand(args);
                    break;
                default:
                    SendHelpText(args.Player);
                    break;
            }
        }

        private void HandleDebugCommand(CommandArgs args)
        {
            if (!args.Player.HasPermission("msc.admin"))
            {
                args.Player.SendErrorMsg("你没有权限使用这个命令。");
                return;
            }

            if (args.Parameters.Count < 2)
            {
                args.Player.SendInfoMsg("用法: /msc debug <on|off|status>");
                return;
            }

            switch (args.Parameters[1].ToLower())
            {
                case "on":
                    NetworkService.SetDebugMode(true);
                    args.Player.SendInfoMsg("调试模式已开启。");
                    break;
                case "off":
                    NetworkService.SetDebugMode(false);
                    args.Player.SendInfoMsg("调试模式已关闭。");
                    break;
                case "status":
                    args.Player.SendInfoMsg($"调试模式当前状态: {(NetworkService.IsDebugMode ? "开启" : "关闭")}");
                    break;
                default:
                    args.Player.SendInfoMsg("无效的参数。使用 on, off, 或 status。");
                    break;
            }
        }

        private void SendHelpText(TSPlayer player)
        {
            var sb = new StringBuilder();
            sb.AppendLine("MultiSCore 命令帮助:");
            sb.AppendLine("/msc tp <服务器名> - 传送到指定的服务器。");
            sb.AppendLine("/msc back - 从子服务器返回主服务器。");
            sb.AppendLine("/msc list - 列出所有可用的服务器。");
            if (player.HasPermission("msc.admin"))
            {
                sb.AppendLine("/msc debug <on|off> - 开启或关闭网络调试模式。");
            }
            player.SendInfoMsg(sb.ToString());
        }
    }
}