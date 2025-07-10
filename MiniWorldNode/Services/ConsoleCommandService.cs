using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MiniWorld.Shared.Models;
using MiniWorldNode.Models;

namespace MiniWorldNode.Services;

/// <summary>
/// 控制台命令服务
/// </summary>
public class ConsoleCommandService : BackgroundService
{
    private readonly ILogger<ConsoleCommandService> _logger;
    private readonly ServerSettings _settings;
    private readonly GameServerManager _serverManager;
    private readonly NodeInfoService _nodeInfoService;

    public ConsoleCommandService(
        ILogger<ConsoleCommandService> logger,
        IOptions<ServerSettings> settings,
        GameServerManager serverManager,
        NodeInfoService nodeInfoService)
    {
        _logger = logger;
        _settings = settings.Value;
        _serverManager = serverManager;
        _nodeInfoService = nodeInfoService;

        WindowsJobManager.Initialize(logger);
    }

    /// <summary>
    /// 执行控制台命令监听
    /// </summary>
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        ShowWelcomeMessage();
        ShowHelp();

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                Console.Write("\n> ");
                var input = Console.ReadLine();

                if (string.IsNullOrWhiteSpace(input))
                    continue;

                await ProcessCommandAsync(input.Trim());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "处理控制台命令时发生错误");
                Console.WriteLine($"错误: {ex.Message}");
            }
        }
    }

    /// <summary>
    /// 处理控制台命令
    /// </summary>
    private async Task ProcessCommandAsync(string input)
    {
        var parts = input.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length == 0) return;

        var command = parts[0].ToLower();

        switch (command)
        {
            case "help":
            case "h":
                ShowHelp();
                break;

            case "status":
            case "s":
                await ShowStatusAsync();
                break;

            case "list":
            case "l":
                ShowServerList();
                break;

            case "start":
                if (parts.Length >= 3)
                {
                    await StartServerLocalAsync(parts[1], parts[2]);
                }
                else
                {
                    Console.WriteLine("用法: start <服务器ID> <地图名称> [最大玩家数]");
                }
                break;

            case "stop":
                if (parts.Length >= 2)
                {
                    var force = parts.Length > 2 && parts[2].ToLower() == "force";
                    await StopServerLocalAsync(parts[1], force);
                }
                else
                {
                    Console.WriteLine("用法: stop <服务器ID> [force]");
                }
                break;

            case "info":
                if (parts.Length >= 2)
                {
                    ShowServerInfo(parts[1]);
                }
                else
                {
                    Console.WriteLine("用法: info <服务器ID>");
                }
                break;

            case "node":
                await ShowNodeInfoAsync();
                break;

            case "createmap":
            case "create-map":
                if (parts.Length >= 2)
                {
                    await CreateMapAsync(parts[1]);
                }
                else
                {
                    Console.WriteLine("用法: createmap <地图名称>");
                }
                break;

            case "deletemap":
            case "delete-map":
                if (parts.Length >= 2)
                {
                    await DeleteMapAsync(parts[1]);
                }
                else
                {
                    Console.WriteLine("用法: deletemap <地图名称>");
                }
                break;

            case "listmaps":
            case "list-maps":
                ListMaps();
                break;

            case "copymap":
            case "copy-map":
                if (parts.Length >= 3)
                {
                    await CopyMapAsync(parts[1], parts[2]);
                }
                else
                {
                    Console.WriteLine("用法: copymap <源地图名称> <目标地图名称>");
                }
                break;

            case "clear":
            case "cls":
                Console.Clear();
                ShowWelcomeMessage();
                break;

            case "exit":
            case "quit":
            case "q":
                Environment.Exit(0);
                break;

            default:
                Console.WriteLine($"未知命令: {command}. 输入 'help' 查看可用命令。");
                break;
        }
    }

    /// <summary>
    /// 本地启动服务器
    /// </summary>
    private async Task StartServerLocalAsync(string serverId, string mapName, int maxPlayers = 8)
    {
        try
        {
            Console.WriteLine($"正在启动服务器: {serverId}...");

            var command = new StartServerCommand
            {
                ServerId = serverId,
                ServerName = $"本地服务器-{serverId}",
                MapName = mapName,
                CreatorUserId = -1, // 本地测试时使用默认用户ID
                MaxPlayers = maxPlayers
            };

            var result = await _serverManager.StartServerAsync(command);
            if (result != null)
            {
                Console.WriteLine($"服务器启动成功!");
                Console.WriteLine($"  服务器ID: {result.ServerId}");
                Console.WriteLine($"  端口: {result.Port}");
                Console.WriteLine($"  地图: {result.MapName}");
                Console.WriteLine($"  状态: {result.Status}");
            }
            else
            {
                Console.WriteLine("服务器启动失败!");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"启动服务器时发生错误: {ex.Message}");
        }
    }

    /// <summary>
    /// 本地停止服务器
    /// </summary>
    private async Task StopServerLocalAsync(string serverId, bool force = false)
    {
        try
        {
            Console.WriteLine($"正在停止服务器: {serverId}...");

            var result = await _serverManager.StopServerAsync(serverId, force);
            if (result)
            {
                Console.WriteLine("服务器停止成功!");
            }
            else
            {
                Console.WriteLine("服务器停止失败或服务器不存在!");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"停止服务器时发生错误: {ex.Message}");
        }
    }

    /// <summary>
    /// 显示状态信息
    /// </summary>
    private async Task ShowStatusAsync()
    {
        try
        {
            var nodeInfo = await _nodeInfoService.GetNodeInfoAsync();

            Console.WriteLine("\n=== 节点状态 ===");
            Console.WriteLine($"节点ID: {nodeInfo.NodeId}");
            Console.WriteLine($"节点名称: {nodeInfo.NodeName}");
            Console.WriteLine($"运行服务器: {nodeInfo.RunningServers}/{nodeInfo.MaxServers}");
            Console.WriteLine($"CPU使用率: {nodeInfo.CpuUsage}%");
            Console.WriteLine($"内存使用率: {nodeInfo.MemoryUsage}%");
            Console.WriteLine($"节点状态: {nodeInfo.Status}");
            Console.WriteLine($"最后更新: {nodeInfo.LastUpdate:yyyy-MM-dd HH:mm:ss}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"获取状态信息时发生错误: {ex.Message}");
        }
    }

    /// <summary>
    /// 显示服务器列表
    /// </summary>
    private void ShowServerList()
    {
        var servers = _serverManager.GetAllServers().ToList();

        Console.WriteLine($"\n=== 服务器列表 ({servers.Count}) ===");

        if (servers.Count == 0)
        {
            Console.WriteLine("暂无运行中的服务器");
            return;
        }

        Console.WriteLine($"{"ID",-15} {"名称",-20} {"地图",-15} {"端口",-8} {"状态",-10} {"启动时间",-20}");
        Console.WriteLine(new string('-', 90));

        foreach (var server in servers)
        {
            Console.WriteLine($"{server.ServerId,-15} {server.ServerName,-20} {server.MapName,-15} {server.Port,-8} {server.Status,-10} {server.StartTime:MM-dd HH:mm:ss}");
        }
    }

    /// <summary>
    /// 显示服务器详细信息
    /// </summary>
    private void ShowServerInfo(string serverId)
    {
        var server = _serverManager.GetServerInfo(serverId);

        if (server == null)
        {
            Console.WriteLine($"服务器不存在: {serverId}");
            return;
        }

        Console.WriteLine($"\n=== 服务器详细信息 ===");
        Console.WriteLine($"服务器ID: {server.ServerId}");
        Console.WriteLine($"服务器名称: {server.ServerName}");
        Console.WriteLine($"地图名称: {server.MapName}");
        Console.WriteLine($"端口: {server.Port}");
        Console.WriteLine($"状态: {server.Status}");
        Console.WriteLine($"进程ID: {server.ProcessId}");
        Console.WriteLine($"启动时间: {server.StartTime:yyyy-MM-dd HH:mm:ss}");
        Console.WriteLine($"地图路径: {server.MapPath}");
        Console.WriteLine($"日志路径: {server.LogPath}");
    }

    /// <summary>
    /// 显示节点信息
    /// </summary>
    private async Task ShowNodeInfoAsync()
    {
        await ShowStatusAsync();
    }

    /// <summary>
    /// 显示欢迎信息
    /// </summary>
    private void ShowWelcomeMessage()
    {
        Console.WriteLine($"\n节点ID: {_settings.NodeId}");
        Console.WriteLine($"节点名称: {_settings.NodeName}");
        Console.WriteLine($"RPC 端口: {_settings.RpcPort}");
        Console.WriteLine("输入 'help' 查看可用命令");
    }

    /// <summary>
    /// 显示帮助信息
    /// </summary>
    private void ShowHelp()
    {
        Console.WriteLine("\n=== 可用命令 ===");
        Console.WriteLine("== 服务器管理 ==");
        Console.WriteLine("help, h          - 显示帮助信息");
        Console.WriteLine("status, s        - 显示节点状态");
        Console.WriteLine("list, l          - 显示服务器列表");
        Console.WriteLine("start <id> <map> - 启动服务器");
        Console.WriteLine("stop <id> [force]- 停止服务器");
        Console.WriteLine("info <id>        - 显示服务器详细信息");
        Console.WriteLine("node             - 显示节点信息");
        Console.WriteLine("\n== 调试命令 ==");
        Console.WriteLine("createmap <name> - 创建新地图（使用UID -1）");
        Console.WriteLine("deletemap <name> - 删除指定地图");
        Console.WriteLine("listmaps         - 列出所有地图文件");
        Console.WriteLine("copymap <src> <dst> - 复制地图文件");
        Console.WriteLine("\n== 系统命令 ==");
        Console.WriteLine("clear, cls       - 清屏");
        Console.WriteLine("exit, quit, q    - 退出程序");
    }

    /// <summary>
    /// 创建新地图（使用默认UID -1）
    /// </summary>
    private async Task CreateMapAsync(string mapName)
    {
        try
        {
            Console.WriteLine($"正在创建地图: {mapName}...");

            const int debugUserId = -1; // 调试用户ID
            var userMapsDirectory = Path.Combine(_settings.MapsDirectory, debugUserId.ToString());
            var targetMapPath = Path.Combine(userMapsDirectory, $"{mapName}.wld");

            // 检查地图是否已存在
            if (File.Exists(targetMapPath))
            {
                Console.WriteLine($"地图 '{mapName}' 已存在: {targetMapPath}");
                return;
            }

            // 确保用户地图目录存在
            if (!Directory.Exists(userMapsDirectory))
            {
                Directory.CreateDirectory(userMapsDirectory);
                Console.WriteLine($"创建目录: {userMapsDirectory}");
            }

            // 复制默认地图
            var defaultMapFile = Directory.GetFiles(_settings.DefaultMapPath, "*.wld").FirstOrDefault();
            if (string.IsNullOrEmpty(defaultMapFile) || !File.Exists(defaultMapFile))
            {
                Console.WriteLine($"错误: 默认地图文件不存在: {_settings.DefaultMapPath}");
                return;
            }

            await Task.Run(() => File.Copy(defaultMapFile, targetMapPath));
            Console.WriteLine($"地图创建成功: {targetMapPath}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"创建地图时发生错误: {ex.Message}");
        }
    }

    /// <summary>
    /// 删除指定地图
    /// </summary>
    private async Task DeleteMapAsync(string mapName)
    {
        try
        {
            Console.WriteLine($"正在删除地图: {mapName}...");

            const int debugUserId = -1; // 调试用户ID
            var userMapsDirectory = Path.Combine(_settings.MapsDirectory, debugUserId.ToString());
            var targetMapPath = Path.Combine(userMapsDirectory, $"{mapName}.wld");

            if (!File.Exists(targetMapPath))
            {
                Console.WriteLine($"地图 '{mapName}' 不存在: {targetMapPath}");
                return;
            }

            // 确认删除
            Console.Write($"确认删除地图 '{mapName}' (y/N): ");
            var input = Console.ReadLine()?.ToLower();
            if (input != "y" && input != "yes")
            {
                Console.WriteLine("删除操作已取消。");
                return;
            }

            await Task.Run(() => File.Delete(targetMapPath));
            Console.WriteLine($"地图删除成功: {targetMapPath}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"删除地图时发生错误: {ex.Message}");
        }
    }

    /// <summary>
    /// 列出所有地图文件
    /// </summary>
    private void ListMaps()
    {
        try
        {
            Console.WriteLine("\n=== 地图文件列表 ===");

            if (!Directory.Exists(_settings.MapsDirectory))
            {
                Console.WriteLine("地图目录不存在");
                return;
            }

            var userDirectories = Directory.GetDirectories(_settings.MapsDirectory);
            if (userDirectories.Length == 0)
            {
                Console.WriteLine("未找到任何用户地图目录");
                return;
            }

            foreach (var userDir in userDirectories)
            {
                var userId = Path.GetFileName(userDir);
                var mapFiles = Directory.GetFiles(userDir, "*.wld");

                Console.WriteLine($"\n用户ID: {userId} ({mapFiles.Length} 个地图)");
                if (mapFiles.Length > 0)
                {
                    foreach (var mapFile in mapFiles)
                    {
                        var mapName = Path.GetFileNameWithoutExtension(mapFile);
                        var fileInfo = new FileInfo(mapFile);
                        Console.WriteLine($"  - {mapName} ({fileInfo.Length / 1024}KB, {fileInfo.LastWriteTime:yyyy-MM-dd HH:mm:ss})");
                    }
                }
                else
                {
                    Console.WriteLine("  (无地图文件)");
                }
            }

            // 显示默认地图
            Console.WriteLine($"\n默认地图目录: {_settings.DefaultMapPath}");
            if (Directory.Exists(_settings.DefaultMapPath))
            {
                var defaultMaps = Directory.GetFiles(_settings.DefaultMapPath, "*.wld");
                if (defaultMaps.Length > 0)
                {
                    foreach (var mapFile in defaultMaps)
                    {
                        var mapName = Path.GetFileName(mapFile);
                        var fileInfo = new FileInfo(mapFile);
                        Console.WriteLine($"  - {mapName} ({fileInfo.Length / 1024}KB)");
                    }
                }
                else
                {
                    Console.WriteLine("  (无默认地图文件)");
                }
            }
            else
            {
                Console.WriteLine("  (默认地图目录不存在)");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"列出地图时发生错误: {ex.Message}");
        }
    }

    /// <summary>
    /// 复制地图文件
    /// </summary>
    private async Task CopyMapAsync(string sourceMapName, string targetMapName)
    {
        try
        {
            Console.WriteLine($"正在复制地图: {sourceMapName} -> {targetMapName}...");

            const int debugUserId = -1; // 调试用户ID
            var userMapsDirectory = Path.Combine(_settings.MapsDirectory, debugUserId.ToString());
            var sourceMapPath = Path.Combine(userMapsDirectory, $"{sourceMapName}.wld");
            var targetMapPath = Path.Combine(userMapsDirectory, $"{targetMapName}.wld");

            if (!File.Exists(sourceMapPath))
            {
                Console.WriteLine($"源地图 '{sourceMapName}' 不存在: {sourceMapPath}");
                return;
            }

            if (File.Exists(targetMapPath))
            {
                Console.WriteLine($"目标地图 '{targetMapName}' 已存在: {targetMapPath}");
                return;
            }

            // 确保用户地图目录存在
            if (!Directory.Exists(userMapsDirectory))
            {
                Directory.CreateDirectory(userMapsDirectory);
                Console.WriteLine($"创建目录: {userMapsDirectory}");
            }

            await Task.Run(() => File.Copy(sourceMapPath, targetMapPath));
            Console.WriteLine($"地图复制成功: {sourceMapPath} -> {targetMapPath}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"复制地图时发生错误: {ex.Message}");
        }
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("服务正在停止，开始关闭所有托管的服务器...");
        await _serverManager.StopAllServersAsync();
        await base.StopAsync(cancellationToken);
        _logger.LogInformation("服务已成功停止。");
    }
}