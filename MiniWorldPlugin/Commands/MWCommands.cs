using MiniWorld.Shared.Models;
using MiniWorldPlugin.Managers;
using MultiSCore.API;
using TShockAPI;

namespace MiniWorldPlugin.Commands
{
    public static class MWCommands
    {
        public static void Register()
        {
            TShockAPI.Commands.ChatCommands.Add(new Command("miniworld.user", MiniWorld, "mw", "miniworld")
            {
                HelpText = "迷你世界指令. 输入 /mw help 查看更多."
            });
        }

        private static void MiniWorld(CommandArgs args)
        {
            var player = args.Player;
            if (player == null)
            {
                return;
            }

            string subCommand;
            if (args.Parameters.Count > 0)
            {
                subCommand = args.Parameters[0].ToLower();
            }
            else
            {
                ShowHelp(args);
                return;
            }

            switch (subCommand)
            {
                case "create":
                    CreateWorld(args);
                    break;
                case "back":
                    if (MSCAPI.IsBeingForwarded(args.Player))
                    {
                        args.Player.BackToHostServer();

                    }
                    else
                    {
                        args.Player.SendErrorMessage($"你未处于任何迷你世界中.");
                    }
                    break;
                case "go":
                    GoToWorld(args);
                    break;
                case "goi":
                    GoToWorldById(args);
                    break;
                case "list":
                    ListWorlds(args);
                    break;
                case "public":
                    SetPublic(args);
                    break;
                case "addeditor":
                    AddEditor(args);
                    break;
                case "deleditor":
                    RemoveEditor(args);
                    break;
                case "stop":
                    StopWorld(args);
                    break;
                case "nodes":
                    if (args.Player.HasPermission("miniworld.admin"))
                        ListNodes(args);
                    else
                        args.Player.SendErrorMessage("你没有权限执行此命令。");
                    break;
                case "listall":
                    if (args.Player.HasPermission("miniworld.admin"))
                        ListAllWorlds(args);
                    else
                        args.Player.SendErrorMessage("你没有权限执行此命令。");
                    break;
                case "reload":
                    if (args.Player.HasPermission("miniworld.admin"))
                    {
                        Config.Reload();
                        args.Player.SendSuccessMessage("MiniWorld 插件配置已重新加载。");
                    }
                    else
                        args.Player.SendErrorMessage("你没有权限执行此命令。");
                    break;
                case "online":
                    ListOnlineWorlds(args);
                    break;
                case "clear":
                    if (args.Player.HasPermission("miniworld.admin"))
                    {
                        var emptyWorlds = WorldManager.Instance.CheckAndStopEmptyWorldsAsync().Result;
                        args.Player.SendSuccessMessage($"已清除 {emptyWorlds.Count} 个无人世界: {string.Join(", ", emptyWorlds.Select(w => $"{w.OwnerName}/{w.WorldName}"))}.");
                    }
                    else
                        args.Player.SendErrorMessage("你没有权限执行此命令。");
                    break;
                // 其他子命令...
                case "help":
                default:
                    ShowHelp(args);
                    break;
            }
        }

        private static void ListOnlineWorlds(CommandArgs args)
        {
            var onlineWorldsInfo = MSCAPI.GetServerStatuses();
            if (onlineWorldsInfo.Count == 0)
            {
                args.Player.SendSuccessMessage("当前没有活跃的迷你世界。");
                return;
            }

            var worldDetails = new List<(MiniWorld.Shared.Models.MiniWorld World, int PlayerCount)>();
            foreach (var info in onlineWorldsInfo)
            {
                var world = WorldManager.Instance.GetAllWorlds().FirstOrDefault(w => w.Port == info.Server.Port);
                if (world != null)
                {
                    worldDetails.Add((world, info.PlayerCount));
                }
            }

            if (worldDetails.Count == 0)
            {
                args.Player.SendSuccessMessage("当前没有活跃的迷你世界。");
                return;
            }

            args.Player.SendSuccessMessage("当前活跃的迷你世界 (按人数排序):");
            int index = 1;
            foreach (var detail in worldDetails.OrderByDescending(d => d.PlayerCount))
            {
                var owner = TShock.UserAccounts.GetUserAccountByID(detail.World.OwnerId);
                var ownerName = owner?.Name ?? "未知";
                args.Player.SendInfoMessage($"{index++}. (ID: {detail.World.Id}) {ownerName}/{detail.World.WorldName} - {detail.PlayerCount} 人");
            }
        }

        private static async void GoToWorld(CommandArgs args)
        {
            if (args.Parameters.Count < 2)
            {
                args.Player.SendErrorMessage("语法错误: /mw go <世界名称> 或 /mw go <玩家名>/<世界名称>");
                return;
            }
            var path = args.Parameters[1];
            string? ownerName = null;
            string worldName;

            if (path.Contains('/'))
            {
                var parts = path.Split('/');
                if (parts.Length != 2 || string.IsNullOrWhiteSpace(parts[0]) || string.IsNullOrWhiteSpace(parts[1]))
                {
                    args.Player.SendErrorMessage("语法错误: /mw go <玩家名>/<世界名称>");
                    return;
                }
                ownerName = parts[0];
                worldName = parts[1];
            }
            else
            {
                worldName = path;
            }



            await WorldManager.Instance.GoToWorld(args.Player, worldName, ownerName);
        }

        private static async void GoToWorldById(CommandArgs args)
        {
            if (args.Parameters.Count < 2)
            {
                args.Player.SendErrorMessage("语法错误: /mw goi <世界ID>");
                return;
            }

            if (!int.TryParse(args.Parameters[1], out var worldId))
            {
                args.Player.SendErrorMessage("无效的世界ID. ID必须是一个数字。");
                return;
            }

            await WorldManager.Instance.GoToWorldById(args.Player, worldId);
        }

        private static async void CreateWorld(CommandArgs args)
        {
            if (!IsPlayerLoggedIn(args.Player)) return;

            if (args.Parameters.Count < 2)
            {
                args.Player.SendErrorMessage("语法错误: /mw create <世界名称>");
                return;
            }

            var worldName = args.Parameters[1];

            // 调用 WorldManager 来创建世界
            await WorldManager.Instance.CreateWorld(args.Player, worldName);
        }

        private static void ListWorlds(CommandArgs args)
        {
            if (!IsPlayerLoggedIn(args.Player)) return;
            var player = args.Player;

            string ownerName;
            if (args.Parameters.Count > 1)
            {
                ownerName = args.Parameters[1];
            }
            else
            {
                ownerName = player.Account.Name;
            }

            var ownerAccount = TShock.UserAccounts.GetUserAccountByName(ownerName);
            if (ownerAccount == null)
            {
                player.SendErrorMessage($"找不到名为 '{ownerName}' 的玩家账户。");
                return;
            }

            var worlds = WorldManager.Instance.GetWorldsByOwner(ownerAccount.ID);
            if (worlds.Count == 0)
            {
                if (ownerAccount.ID == player.Account.ID)
                    player.SendSuccessMessage("你还没有任何迷你世界。使用 /mw create 来创建一个吧！");
                else
                    player.SendSuccessMessage($"玩家 '{ownerName}' 还没有任何迷你世界。");
                return;
            }

            player.SendSuccessMessage($"玩家 '{ownerName}' 的迷你世界列表:");
            foreach (var world in worlds)
            {
                var statusColor = world.Status == WorldStatus.Online ? "90EE90" : "D3D3D3"; // LightGreen for Online, LightGray otherwise
                player.SendInfoMessage($" - (ID: {world.Id}) {world.WorldName} [c/{statusColor}:{world.Status}]");
            }
        }

        private static void SetPublic(CommandArgs args)
        {
            if (!IsPlayerLoggedIn(args.Player)) return;

            if (args.Parameters.Count < 3)
            {
                args.Player.SendErrorMessage("语法错误: /mw public <世界名称> <on|off>");
                return;
            }

            var worldName = args.Parameters[1];
            var state = args.Parameters[2].ToLower();
            bool isPublic;

            if (state == "on")
            {
                isPublic = true;
            }
            else if (state == "off")
            {
                isPublic = false;
            }
            else
            {
                args.Player.SendErrorMessage("无效状态。请使用 'on' 或 'off'。");
                return;
            }

            WorldManager.Instance.SetPublicStatus(args.Player, worldName, isPublic);
        }

        private static void AddEditor(CommandArgs args)
        {
            if (!IsPlayerLoggedIn(args.Player)) return;

            if (args.Parameters.Count < 3)
            {
                args.Player.SendErrorMessage("语法错误: /mw addeditor <世界名称> <玩家名称>");
                return;
            }
            var worldName = args.Parameters[1];
            var editorName = args.Parameters[2];
            WorldManager.Instance.AddEditor(args.Player, worldName, editorName);
        }

        private static void RemoveEditor(CommandArgs args)
        {
            if (!IsPlayerLoggedIn(args.Player)) return;

            if (args.Parameters.Count < 3)
            {
                args.Player.SendErrorMessage("语法错误: /mw deleditor <世界名称> <玩家名称>");
                return;
            }
            var worldName = args.Parameters[1];
            var editorName = args.Parameters[2];
            WorldManager.Instance.RemoveEditor(args.Player, worldName, editorName);
        }

        private static async void StopWorld(CommandArgs args)
        {
            if (!IsPlayerLoggedIn(args.Player)) return;

            if (args.Parameters.Count < 2)
            {
                args.Player.SendErrorMessage("语法错误: /mw stop <世界名称>");
                return;
            }
            var worldName = args.Parameters[1];
            await WorldManager.Instance.StopWorld(args.Player, worldName);
        }

        private static void ListNodes(CommandArgs args)
        {
            var nodeStatus = NodeManager.Instance.GetNodeStatusSummary();
            args.Player.SendSuccessMessage("节点状态:");
            args.Player.SendInfoMessage(nodeStatus);

            if (NodeManager.Instance.CurrentNode != null)
            {
                var node = NodeManager.Instance.CurrentNode;
                args.Player.SendInfoMessage(
                  $"节点详情:\n" +
                  $" - 名称: {node.NodeName} ({node.NodeId})\n" +
                  $" - 状态: {node.Status}, 负载: {node.RunningServers}/{node.MaxServers} 服务器\n" +
                  $" - CPU: {node.CpuUsage:F2}%, 内存: {node.MemoryUsage:F2}%\n" +
                  $" - 最后更新: {node.LastUpdate.ToLocalTime()}");
            }
        }

        private static void ListAllWorlds(CommandArgs args)
        {
            var worlds = WorldManager.Instance.GetAllWorlds();
            if (worlds.Count == 0)
            {
                args.Player.SendSuccessMessage("服务器上还没有任何迷你世界。");
                return;
            }

            args.Player.SendSuccessMessage($"服务器上的所有迷你世界 ({worlds.Count}):");
            foreach (var world in worlds)
            {
                var statusColor = world.Status == WorldStatus.Online ? "90EE90" : "D3D3D3";
                args.Player.SendInfoMessage($" - {world.WorldName} (所有者: {world.OwnerName}) [c/{statusColor}:{world.Status}]");
            }
        }

        private static void ShowHelp(CommandArgs args)
        {
            args.Player.SendSuccessMessage("迷你世界 帮助菜单:");
            args.Player.SendInfoMessage("/mw create <世界名称> - 创建一个新的迷你世界.");
            args.Player.SendInfoMessage("/mw back - 返回主服.");
            args.Player.SendInfoMessage("/mw list [玩家名] - 列出你或指定玩家拥有的所有迷你世界.");
            args.Player.SendInfoMessage("/mw online - 列出所有有玩家在线的世界.");
            args.Player.SendInfoMessage("/mw go <[玩家名]/世界名> - 前往一个迷你世界，如果世界离线则会自动启动.");
            args.Player.SendInfoMessage("/mw goi <世界ID> - 通过ID前往一个迷你世界.");
            args.Player.SendInfoMessage("/mw public <世界名> <on|off> - 设置世界的公开状态.");
            args.Player.SendInfoMessage("/mw addeditor <世界名> <玩家名> - 添加编辑者.");
            args.Player.SendInfoMessage("/mw deleditor <世界名> <玩家名> - 移除编辑者.");
            args.Player.SendInfoMessage("/mw stop <世界名> - 停止你的世界.");
            if (args.Player.HasPermission("miniworld.admin"))
            {
                args.Player.SendInfoMessage("/mw nodes - (管理员) 查看节点信息.");
                args.Player.SendInfoMessage("/mw listall - (管理员) 查看所有世界.");
                args.Player.SendInfoMessage("/mw reload - (管理员) 重新加载插件配置.");
                args.Player.SendInfoMessage("/mw clear - (管理员) 清除所有无人在线的世界.");
            }
        }

        private static bool IsPlayerLoggedIn(TSPlayer player)
        {
            if (player.Account == null)
            {
                player.SendErrorMessage("请先登录以执行此操作。");
                return false;
            }
            return true;
        }
    }
}