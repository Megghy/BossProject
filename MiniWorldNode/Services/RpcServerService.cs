using System.Net;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MiniWorld.Shared;
using MiniWorld.Shared.Models;
using MiniWorldNode.Models;
using StreamJsonRpc;

namespace MiniWorldNode.Services;

/// <summary>
/// 基于 StreamJsonRpc 的 RPC 服务器服务
/// </summary>
public class RpcServerService : BackgroundService, IMiniWorldNodeApi
{
    private readonly ILogger<RpcServerService> _logger;
    private readonly ServerSettings _settings;
    private readonly GameServerManager _serverManager;
    private readonly NodeInfoService _nodeInfoService;
    private HttpListener? _httpListener;
    private readonly List<JsonRpc> _activeConnections = new();
    private readonly object _connectionsLock = new();

    public RpcServerService(
        ILogger<RpcServerService> logger,
        IOptions<ServerSettings> settings,
        GameServerManager serverManager,
        NodeInfoService nodeInfoService)
    {
        _logger = logger;
        _settings = settings.Value;
        _serverManager = serverManager;
        _nodeInfoService = nodeInfoService;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            await StartRpcServerAsync(stoppingToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "RPC 服务器运行时发生错误");
        }
    }

    private async Task StartRpcServerAsync(CancellationToken stoppingToken)
    {
        var port = _settings.RpcPort ?? 8080;
#if DEBUG
        var url = $"http://localhost:{port}/rpc/";
#else
        var url = $"http://*:{port}/rpc/";
#endif

        _httpListener = new HttpListener();
        _httpListener.Prefixes.Add(url);

        try
        {
            _httpListener.Start();
            _logger.LogInformation($"RPC 服务器已在 {url} 启动");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    var context = await _httpListener.GetContextAsync();
                    _ = Task.Run(() => HandleClientAsync(context, stoppingToken), stoppingToken);
                }
                catch (HttpListenerException) when (stoppingToken.IsCancellationRequested)
                {
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "处理客户端连接时发生错误");
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "启动 RPC 服务器时发生错误");
        }
        finally
        {
            _httpListener?.Stop();
        }
    }

    private async Task HandleClientAsync(HttpListenerContext context, CancellationToken cancellationToken)
    {
        try
        {
            if (!context.Request.IsWebSocketRequest)
            {
                _logger.LogWarning("收到非 WebSocket 连接请求，已拒绝。");
                context.Response.StatusCode = 400; // Bad Request
                context.Response.Close();
                return;
            }

            _logger.LogInformation($"新的客户端 WebSocket 连接: {context.Request.RemoteEndPoint}");

            var webSocketContext = await context.AcceptWebSocketAsync(subProtocol: null);
            var webSocket = webSocketContext.WebSocket;

            var rpc = new JsonRpc(new WebSocketMessageHandler(webSocket), this);

            lock (_connectionsLock)
            {
                _activeConnections.Add(rpc);
            }

            // 必须在添加到列表后启动监听，以防竞态条件
            rpc.StartListening();

            try
            {
                // 等待连接完成或取消
                await rpc.Completion.ConfigureAwait(false);
            }
            finally
            {
                lock (_connectionsLock)
                {
                    _activeConnections.Remove(rpc);
                }

                rpc.Dispose();
                _logger.LogInformation($"客户端 WebSocket 连接已断开: {context.Request.RemoteEndPoint}");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "处理客户端 WebSocket 连接时发生错误");
        }
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("正在停止 RPC 服务器...");

        // 关闭所有活动连接
        lock (_connectionsLock)
        {
            foreach (var connection in _activeConnections.ToList())
            {
                try
                {
                    connection.Dispose();
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "关闭 RPC 连接时发生错误");
                }
            }
            _activeConnections.Clear();
        }

        // 停止 HTTP 监听器
        _httpListener?.Stop();

        await base.StopAsync(cancellationToken);
    }

    #region IMiniWorldNodeApi Implementation

    public async Task<NodeInfo> GetNodeInfoAsync()
    {
        _logger.LogDebug("收到获取节点信息请求");
        return await _nodeInfoService.GetNodeInfoAsync();
    }

    public async Task<List<GameServerInfo>> GetServersInfoAsync()
    {
        _logger.LogDebug("收到获取所有服务器信息请求");
        return await Task.FromResult(_serverManager.GetAllServers().ToList());
    }

    public async Task<GameServerInfo?> GetServerInfoAsync(string serverId)
    {
        _logger.LogDebug($"收到获取服务器信息请求: {serverId}");
        return await Task.FromResult(_serverManager.GetServerInfo(serverId));
    }

    public async Task<GameServerInfo> StartServerAsync(StartServerCommand command)
    {
        _logger.LogInformation($"收到启动服务器请求: {command.ServerId}");

        var result = await _serverManager.StartServerAsync(command);
        if (result == null)
        {
            throw new InvalidOperationException($"启动服务器失败: {command.ServerId}");
        }

        return result;
    }

    public async Task<bool> StopServerAsync(StopServerCommand command)
    {
        _logger.LogInformation($"收到停止服务器请求: {command.ServerId}");
        return await _serverManager.StopServerAsync(command.ServerId, command.ForceStop);
    }

    public async Task<string[]> ListMapsAsync(int creatorUserId)
    {
        _logger.LogDebug($"收到列出地图请求: 用户ID {creatorUserId}");

        try
        {
            var mapsDirectory = Path.Combine("Maps", creatorUserId.ToString());
            if (!Directory.Exists(mapsDirectory))
            {
                return Array.Empty<string>();
            }

            var worldFiles = Directory.GetFiles(mapsDirectory, "*.wld");
            return worldFiles.Select(Path.GetFileNameWithoutExtension).ToArray();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"列出地图时发生错误: 用户ID {creatorUserId}");
            return Array.Empty<string>();
        }
    }

    public async Task<bool> CreateMapAsync(int creatorUserId, string mapName)
    {
        _logger.LogInformation($"收到创建地图请求: {mapName} (用户ID: {creatorUserId})");

        try
        {
            // 使用与 GameServerManager 相同的逻辑
            var userMapsDir = Path.Combine("Maps", creatorUserId.ToString());
            Directory.CreateDirectory(userMapsDir);

            var targetMapPath = Path.Combine(userMapsDir, $"{mapName}.wld");
            if (File.Exists(targetMapPath))
            {
                _logger.LogWarning($"地图已存在: {targetMapPath}");
                return false;
            }

            // 从默认地图复制
            var defaultMapPath = Path.Combine("Maps", "default.wld");
            if (!File.Exists(defaultMapPath))
            {
                _logger.LogError($"默认地图不存在: {defaultMapPath}");
                return false;
            }

            File.Copy(defaultMapPath, targetMapPath);
            _logger.LogInformation($"地图创建成功: {targetMapPath}");
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"创建地图时发生错误: {mapName}");
            return false;
        }
    }

    public async Task<bool> DeleteMapAsync(int creatorUserId, string mapName)
    {
        _logger.LogInformation($"收到删除地图请求: {mapName} (用户ID: {creatorUserId})");

        try
        {
            var mapPath = Path.Combine("Maps", creatorUserId.ToString(), $"{mapName}.wld");
            if (!File.Exists(mapPath))
            {
                _logger.LogWarning($"地图不存在: {mapPath}");
                return false;
            }

            File.Delete(mapPath);
            _logger.LogInformation($"地图删除成功: {mapPath}");
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"删除地图时发生错误: {mapName}");
            return false;
        }
    }

    public async Task<bool> CopyMapAsync(int creatorUserId, string sourceMapName, string destMapName)
    {
        _logger.LogInformation($"收到复制地图请求: {sourceMapName} -> {destMapName} (用户ID: {creatorUserId})");

        try
        {
            var userMapsDir = Path.Combine("Maps", creatorUserId.ToString());
            var sourcePath = Path.Combine(userMapsDir, $"{sourceMapName}.wld");
            var destPath = Path.Combine(userMapsDir, $"{destMapName}.wld");

            if (!File.Exists(sourcePath))
            {
                _logger.LogWarning($"源地图不存在: {sourcePath}");
                return false;
            }

            if (File.Exists(destPath))
            {
                _logger.LogWarning($"目标地图已存在: {destPath}");
                return false;
            }

            File.Copy(sourcePath, destPath);
            _logger.LogInformation($"地图复制成功: {sourcePath} -> {destPath}");
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"复制地图时发生错误: {sourceMapName} -> {destMapName}");
            return false;
        }
    }

    #endregion
}