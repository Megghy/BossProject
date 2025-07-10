using System.Collections.Concurrent;
using System.Diagnostics;
using System.Net.NetworkInformation;
using System.Runtime.InteropServices;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MiniWorld.Shared.Models;
using MiniWorldNode.Models;
using Newtonsoft.Json;

namespace MiniWorldNode.Services;

/// <summary>
/// 游戏服务器管理服务
/// </summary>
public class GameServerManager
{
    public delegate void ServerStatusUpdateHandler(string serverId, string status, string message);
    public event ServerStatusUpdateHandler OnServerStatusUpdate;

    private readonly ILogger<GameServerManager> _logger;
    private readonly ServerSettings _settings;
    private readonly ConcurrentDictionary<string, GameServerInfo> _servers = new();
    private readonly SemaphoreSlim _portSemaphore = new(1, 1);

#if WINDOWS
    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool GenerateConsoleCtrlEvent(uint dwCtrlEvent, uint dwProcessGroupId);
    private const uint CTRL_C_EVENT = 0;
#endif

    public GameServerManager(ILogger<GameServerManager> logger, IOptions<ServerSettings> settings)
    {
        _logger = logger;
        _settings = settings.Value;

        // 验证配置和确保必要的目录存在
        ValidateConfigurationAndEnsureDirectories();
    }

    /// <summary>
    /// 启动游戏服务器
    /// </summary>
    public async Task<GameServerInfo?> StartServerAsync(StartServerCommand command)
    {
        // 输入验证
        if (command == null)
        {
            _logger.LogError("StartServerCommand 不能为空");
            return null;
        }

        if (string.IsNullOrWhiteSpace(command.ServerId))
        {
            _logger.LogError("ServerId 不能为空");
            return null;
        }

        if (string.IsNullOrWhiteSpace(command.ServerName))
        {
            _logger.LogError("ServerName 不能为空");
            return null;
        }

        if (string.IsNullOrWhiteSpace(command.MapName))
        {
            _logger.LogError("MapName 不能为空");
            return null;
        }

        try
        {
            _logger.LogInformation($"准备启动服务器: {command.ServerName} (地图: {command.MapName})");
            OnServerStatusUpdate?.Invoke(command.ServerId, "Starting", $"准备启动服务器: {command.ServerName}");

            // 检查服务器是否已经存在
            if (_servers.ContainsKey(command.ServerId))
            {
                _logger.LogWarning($"服务器 {command.ServerId} 已经存在");
                return _servers[command.ServerId];
            }

            // 验证 TShock 程序是否存在
            if (!ValidateTShockPath())
            {
                OnServerStatusUpdate?.Invoke(command.ServerId, "Error", "TShock 程序不存在或无法访问");
                return null;
            }

            // 准备地图文件
            OnServerStatusUpdate?.Invoke(command.ServerId, "PreparingMap", "正在准备世界文件...");
            var mapPath = await PrepareMapAsync(command.MapName, command.CreatorUserId);
            if (string.IsNullOrEmpty(mapPath))
            {
                _logger.LogError($"准备地图文件失败: {command.MapName}, 用户ID: {command.CreatorUserId}");
                OnServerStatusUpdate?.Invoke(command.ServerId, "Error", $"准备地图文件失败: {command.MapName}");
                return null;
            }

            // 获取可用端口
            OnServerStatusUpdate?.Invoke(command.ServerId, "FindingPort", "正在寻找可用端口...");
            var port = await GetAvailablePortAsync();
            if (port == -1)
            {
                _logger.LogError("无法找到可用端口");
                OnServerStatusUpdate?.Invoke(command.ServerId, "Error", "无法找到可用端口");
                return null;
            }
            else
            {
                _logger.LogInformation($"可用端口: {port}");
            }

            // 准备日志路径
            var logPath = PrepareLogPath(command.ServerId);
            if (string.IsNullOrEmpty(logPath))
            {
                OnServerStatusUpdate?.Invoke(command.ServerId, "Error", "无法创建日志文件");
                return null;
            }

            var serverInfo = new GameServerInfo
            {
                ServerId = command.ServerId,
                ServerName = command.ServerName,
                MapName = command.MapName,
                Port = port,
                Status = ServerStatus.Starting,
                MapPath = mapPath,
                LogPath = logPath,
                StartTime = DateTime.Now
            };

            // 启动TShock进程
            OnServerStatusUpdate?.Invoke(command.ServerId, "LaunchingProcess", "正在启动...");
            var process = await StartTShockProcessAsync(serverInfo, command);
            if (process == null)
            {
                // StartTShockProcessAsync 内部已经处理了日志和状态更新
                return null;
            }

            serverInfo.ProcessId = process.Id;
            serverInfo.Process = process; // 保存进程引用
            serverInfo.Status = ServerStatus.Running;

            // 添加到服务器列表
            _servers[command.ServerId] = serverInfo;

            _logger.LogInformation($"服务器启动成功: {command.ServerName} (端口: {port}, PID: {process.Id})");
            var payload = new { Message = "启动成功", Port = serverInfo.Port };
            OnServerStatusUpdate?.Invoke(command.ServerId, "Online", JsonConvert.SerializeObject(payload));
            return serverInfo;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"启动服务器时发生错误: {command.ServerId}");
            OnServerStatusUpdate?.Invoke(command.ServerId, "Error", $"启动时发生未知错误: {ex.Message}");
            return null;
        }
    }

    /// <summary>
    /// 停止游戏服务器
    /// </summary>
    public async Task<bool> StopServerAsync(string serverId, bool forceStop = false)
    {
        if (string.IsNullOrWhiteSpace(serverId))
        {
            _logger.LogError("ServerId 不能为空");
            return false;
        }

        try
        {
            if (!_servers.TryGetValue(serverId, out var serverInfo))
            {
                _logger.LogWarning($"服务器不存在: {serverId}");
                return false;
            }

            _logger.LogInformation($"准备停止服务器: {serverInfo.ServerName}");
            serverInfo.Status = ServerStatus.Stopping;

            // 优先使用保存的进程引用
            Process? process = serverInfo.Process;

            // 如果没有保存的进程引用，尝试通过PID获取
            if (process == null && serverInfo.ProcessId > 0)
            {
                try
                {
                    process = Process.GetProcessById(serverInfo.ProcessId);
                }
                catch (ArgumentException)
                {
                    _logger.LogInformation($"进程 {serverInfo.ProcessId} 不存在，可能已经退出");
                    serverInfo.Status = ServerStatus.Stopped;
                    serverInfo.Process = null;
                    _servers.TryRemove(serverId, out _);
                    return true;
                }
            }

            if (process != null)
            {
                try
                {
                    // 检查进程是否还在运行
                    if (process.HasExited)
                    {
                        _logger.LogInformation($"进程 {serverId} 已经退出，退出代码: {process.ExitCode}");
                        process.Dispose();
                        serverInfo.Process = null;
                        serverInfo.Status = ServerStatus.Stopped;
                        _servers.TryRemove(serverId, out _);
                        return true;
                    }

                    if (forceStop)
                    {
                        process.Kill();
                        _logger.LogInformation($"强制终止服务器进程: {serverId}");
                    }
                    else
                    {
                        _logger.LogInformation($"正在向服务器 {serverId} 发送关闭信号...");
                        bool signalSent = false;

                        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                        {
#if WINDOWS
                            try
                            {
                                if (GenerateConsoleCtrlEvent(CTRL_C_EVENT, (uint)process.Id))
                                {
                                    _logger.LogInformation($"已向进程 {process.Id} 发送 CTRL+C 信号。");
                                    signalSent = true;
                                }
                                else
                                {
                                    _logger.LogWarning($"发送 CTRL+C 信号失败，错误码: {Marshal.GetLastWin32Error()}。将尝试 'exit' 命令。");
                                }
                            }
                            catch (Exception ex)
                            {
                                _logger.LogWarning(ex, "发送 CTRL+C 信号时发生异常。将尝试 'exit' 命令。");
                            }
#endif
                        }

                        if (!signalSent && process.StartInfo.RedirectStandardInput && process.StandardInput.BaseStream.CanWrite)
                        {
                            try
                            {
                                await process.StandardInput.WriteLineAsync("exit");
                                await process.StandardInput.FlushAsync();
                                _logger.LogInformation($"已向进程 {process.Id} 发送 'exit' 命令。");
                                signalSent = true;
                            }
                            catch (InvalidOperationException ex)
                            {
                                _logger.LogWarning(ex, $"无法向进程 {serverId} 写入 'exit' 命令。");
                            }
                        }

                        if (signalSent)
                        {
                            // 等待进程退出
                            bool exited = await Task.Run(() => process.WaitForExit(5000)); // 延长等待时间
                            if (!exited)
                            {
                                _logger.LogWarning($"服务器 {serverId} 未能在15秒内正常退出，将强制终止。");
                                process.Kill();
                            }
                            else
                            {
                                _logger.LogInformation($"服务器 {serverId} 已成功退出。");
                            }
                        }
                        else
                        {
                            _logger.LogWarning($"无法向进程 {serverId} 发送任何关闭信号，将强制终止。");
                            process.Kill();
                        }
                    }

                    // 等待进程完全退出
                    if (!process.HasExited)
                    {
                        await Task.Run(() => process.WaitForExit(3000));
                    }

                    // 清理进程资源
                    process.Dispose();
                    serverInfo.Process = null;
                    _servers.TryRemove(serverId, out _);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"停止进程 {serverId} 时发生错误");
                    try
                    {
                        if (!process.HasExited)
                        {
                            process.Kill();
                        }
                        process.Dispose();
                    }
                    catch
                    {
                        // 忽略清理时的错误
                    }
                    serverInfo.Process = null;
                }
            }

            serverInfo.Status = ServerStatus.Stopped;
            serverInfo.Process = null; // 清理进程引用
            _servers.TryRemove(serverId, out _);

            _logger.LogInformation($"服务器停止成功: {serverInfo.ServerName}");
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"停止服务器时发生错误: {serverId}");
            return false;
        }
    }

    /// <summary>
    /// 获取服务器信息
    /// </summary>
    public GameServerInfo? GetServerInfo(string serverId)
    {
        if (string.IsNullOrWhiteSpace(serverId))
        {
            return null;
        }

        return _servers.TryGetValue(serverId, out var info) ? info : null;
    }

    /// <summary>
    /// 获取所有服务器信息
    /// </summary>
    public IEnumerable<GameServerInfo> GetAllServers()
    {
        return _servers.Values.ToList();
    }

    /// <summary>
    /// 停止所有游戏服务器
    /// </summary>
    public async Task StopAllServersAsync()
    {
        _logger.LogInformation("正在停止所有托管的服务器...");
        var runningServers = _servers.Keys.ToList();
        if (runningServers.Count == 0)
        {
            _logger.LogInformation("没有正在运行的服务器需要停止。");
            return;
        }

        var stopTasks = runningServers.Select(id => StopServerAsync(id, true)).ToList();
        await Task.WhenAll(stopTasks);

        _logger.LogInformation("所有服务器均已停止。");
    }

    /// <summary>
    /// 同步停止所有游戏服务器（用于程序退出事件）
    /// </summary>
    public void StopAllServers()
    {
        _logger.LogInformation("正在同步停止所有托管的服务器...");
        var runningServers = _servers.Values.Where(s => s.ProcessId != 0).ToList();
        if (runningServers.Count == 0)
        {
            return;
        }

        // 1. 向所有进程发送关闭信号
        foreach (var serverInfo in runningServers)
        {
            try
            {
                // 优先使用保存的进程引用
                var process = serverInfo.Process;
                if (process == null && serverInfo.ProcessId > 0)
                {
                    try
                    {
                        process = Process.GetProcessById(serverInfo.ProcessId);
                    }
                    catch (ArgumentException)
                    {
                        _logger.LogInformation($"进程 {serverInfo.ProcessId} 不存在，跳过关闭信号");
                        continue;
                    }
                }

                if (process != null && !process.HasExited)
                {
                    _logger.LogInformation($"正在向服务器 {serverInfo.ServerId} 发送关闭信号...");
                    bool signalSent = false;

                    if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                    {
#if WINDOWS
                        try
                        {
                            if (GenerateConsoleCtrlEvent(CTRL_C_EVENT, (uint)process.Id))
                            {
                                _logger.LogInformation($"已向进程 {process.Id} 发送 CTRL+C 信号。");
                                signalSent = true;
                            }
                            else
                            {
                                _logger.LogWarning($"发送 CTRL+C 信号失败，错误码: {Marshal.GetLastWin32Error()}。将尝试 'exit' 命令。");
                            }
                        }
                        catch (Exception ex)
                        {
                            _logger.LogWarning(ex, "发送 CTRL+C 信号时发生异常。将尝试 'exit' 命令。");
                        }
#endif
                    }

                    // 如果发送信号失败或者不是在Windows平台，则尝试发送 'exit' 命令
                    if (!signalSent && process.StartInfo.RedirectStandardInput)
                    {
                        try
                        {
                            process.StandardInput.WriteLine("exit");
                            process.StandardInput.Flush();
                            _logger.LogInformation($"已向进程 {process.Id} 发送 'exit' 命令。");
                        }
                        catch (InvalidOperationException)
                        {
                            _logger.LogWarning($"无法向进程 {serverInfo.ServerId} 写入 'exit' 命令。");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"同步发送关闭信号至服务器 {serverInfo.ServerId} 时发生错误");
            }
        }

        // 2. 等待一小段时间
        _logger.LogInformation("等待10秒以便服务器正常关闭...");
        Thread.Sleep(10000); // 延长等待时间

        // 3. 强制终止仍在运行的进程
        foreach (var serverInfo in runningServers)
        {
            try
            {
                // 优先使用保存的进程引用
                var process = serverInfo.Process;
                if (process == null && serverInfo.ProcessId > 0)
                {
                    try
                    {
                        process = Process.GetProcessById(serverInfo.ProcessId);
                    }
                    catch (ArgumentException)
                    {
                        // 进程已经退出，更新状态
                        serverInfo.Status = ServerStatus.Stopped;
                        serverInfo.Process = null;
                        continue;
                    }
                }

                if (process != null && !process.HasExited)
                {
                    _logger.LogWarning($"服务器 {serverInfo.ServerId} 未能快速退出，将强制终止");
                    process.Kill();

                    // 等待进程完全终止
                    try
                    {
                        process.WaitForExit(3000);
                    }
                    catch
                    {
                        // 忽略等待超时
                    }
                }

                // 清理资源
                process?.Dispose();
                serverInfo.Process = null;
                serverInfo.Status = ServerStatus.Stopped;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"同步停止服务器 {serverInfo.ServerId} 时发生错误");
                // 尝试清理资源
                if (serverInfo.Process != null)
                {
                    try
                    {
                        serverInfo.Process.Dispose();
                        serverInfo.Process = null;
                    }
                    catch
                    {
                        // 忽略清理错误
                    }
                }
                serverInfo.Status = ServerStatus.Stopped;
            }
        }
        _logger.LogInformation("所有服务器均已通过同步方式停止。");
    }

    /// <summary>
    /// 准备地图文件
    /// </summary>
    private async Task<string> PrepareMapAsync(string mapName, int creatorUserId)
    {
        try
        {
            // 验证地图名称
            if (string.IsNullOrWhiteSpace(mapName))
            {
                _logger.LogError("地图名称不能为空");
                return string.Empty;
            }

            // 验证地图名称是否包含非法字符
            if (mapName.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0)
            {
                _logger.LogError($"地图名称包含非法字符: {mapName}");
                return string.Empty;
            }

            // 根据创建者UID创建子目录
            var userMapsDirectory = Path.Combine(_settings.MapsDirectory, creatorUserId.ToString());
            var targetMapPath = Path.Combine(userMapsDirectory, $"{mapName}.wld");

            // 确保用户地图目录存在
            if (!Directory.Exists(userMapsDirectory))
            {
                try
                {
                    Directory.CreateDirectory(userMapsDirectory);
                    _logger.LogInformation($"创建用户地图目录: {userMapsDirectory}");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"创建用户地图目录失败: {userMapsDirectory}");
                    return string.Empty;
                }
            }

            // 如果目标地图已存在，验证文件完整性
            if (File.Exists(targetMapPath))
            {
                try
                {
                    var fileInfo = new FileInfo(targetMapPath);
                    if (fileInfo.Length > 0)
                    {
                        _logger.LogInformation($"使用现有地图: {targetMapPath}");
                        return targetMapPath;
                    }
                    else
                    {
                        _logger.LogWarning($"现有地图文件为空，将重新创建: {targetMapPath}");
                        File.Delete(targetMapPath);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"检查现有地图文件时发生错误: {targetMapPath}");
                    return string.Empty;
                }
            }

            // 验证默认地图路径
            if (!Directory.Exists(_settings.DefaultMapPath))
            {
                _logger.LogError($"默认地图目录不存在: {_settings.DefaultMapPath}");
                return string.Empty;
            }

            // 复制默认地图
            var defaultMapFile = Directory.GetFiles(_settings.DefaultMapPath, "*.wld").FirstOrDefault();
            if (string.IsNullOrEmpty(defaultMapFile) || !File.Exists(defaultMapFile))
            {
                _logger.LogError($"默认地图文件不存在: {_settings.DefaultMapPath}");
                return string.Empty;
            }

            // 验证默认地图文件大小
            var defaultMapInfo = new FileInfo(defaultMapFile);
            if (defaultMapInfo.Length == 0)
            {
                _logger.LogError($"默认地图文件为空: {defaultMapFile}");
                return string.Empty;
            }

            await Task.Run(() => File.Copy(defaultMapFile, targetMapPath, true));

            // 验证复制是否成功
            if (!File.Exists(targetMapPath))
            {
                _logger.LogError($"地图文件复制失败: {defaultMapFile} -> {targetMapPath}");
                return string.Empty;
            }

            var copiedMapInfo = new FileInfo(targetMapPath);
            if (copiedMapInfo.Length != defaultMapInfo.Length)
            {
                _logger.LogError($"地图文件复制不完整: 原始大小 {defaultMapInfo.Length}, 复制后大小 {copiedMapInfo.Length}");
                return string.Empty;
            }

            _logger.LogInformation($"复制地图文件成功: {defaultMapFile} -> {targetMapPath}");
            return targetMapPath;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"准备地图文件时发生错误: {mapName}, 用户ID: {creatorUserId}");
            return string.Empty;
        }
    }

    /// <summary>
    /// 获取可用端口
    /// </summary>
    private async Task<int> GetAvailablePortAsync()
    {
        if (_settings.BasePort <= 0 || _settings.BasePort > 65535)
        {
            _logger.LogError($"基础端口配置无效: {_settings.BasePort}");
            return -1;
        }

        await _portSemaphore.WaitAsync();
        try
        {
            var maxPort = Math.Min(_settings.BasePort + 100, 65535);
            for (int port = _settings.BasePort; port <= maxPort; port++)
            {
                if (IsPortAvailable(port))
                {
                    _logger.LogDebug($"找到可用端口: {port}");
                    return port;
                }
            }

            _logger.LogError($"在端口范围 {_settings.BasePort}-{maxPort} 内未找到可用端口");
            return -1;
        }
        finally
        {
            _portSemaphore.Release();
        }
    }

    /// <summary>
    /// 检查端口是否可用
    /// </summary>
    private static bool IsPortAvailable(int port)
    {
        try
        {
            var ipGlobalProperties = IPGlobalProperties.GetIPGlobalProperties();
            var tcpConnInfoArray = ipGlobalProperties.GetActiveTcpListeners();
            var udpConnInfoArray = ipGlobalProperties.GetActiveUdpListeners();

            // 检查TCP和UDP端口是否被占用
            return !tcpConnInfoArray.Any(tcpi => tcpi.Port == port) &&
                   !udpConnInfoArray.Any(udpi => udpi.Port == port);
        }
        catch (Exception)
        {
            // 如果无法检查端口状态，假设端口不可用
            return false;
        }
    }

    /// <summary>
    /// 启动TShock进程
    /// </summary>
    private async Task<Process?> StartTShockProcessAsync(GameServerInfo serverInfo, StartServerCommand command)
    {
        try
        {
            var workingDirectory = Path.GetDirectoryName(_settings.TShockPath);
            if (string.IsNullOrEmpty(workingDirectory) || !Directory.Exists(workingDirectory))
            {
                _logger.LogError($"TShock 工作目录不存在: {workingDirectory}");
                return null;
            }

            var startInfo = new ProcessStartInfo
            {
                FileName = _settings.TShockPath,
                Arguments = BuildTShockArguments(serverInfo, command),
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                RedirectStandardInput = true,
                WorkingDirectory = workingDirectory
            };

            var process = new Process { StartInfo = startInfo, EnableRaisingEvents = true };

            // 监听进程退出事件
            process.Exited += (sender, e) =>
            {
                try
                {
                    var exitCode = process.HasExited ? process.ExitCode : -1;
                    _logger.LogWarning($"服务器进程退出: {serverInfo.ServerId}, 退出代码: {exitCode}");

                    if (_servers.TryGetValue(serverInfo.ServerId, out var info))
                    {
                        info.Status = ServerStatus.Stopped;
                        info.Process = null; // 清理进程引用
                        OnServerStatusUpdate?.Invoke(serverInfo.ServerId, "Stopped",
                            $"服务器进程退出，退出代码: {exitCode}");
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"处理进程退出事件时发生错误: {serverInfo.ServerId}");
                }
            };

            bool started = process.Start();
            if (!started)
            {
                _logger.LogError("无法启动TShock进程");
                OnServerStatusUpdate?.Invoke(serverInfo.ServerId, "Error", "无法启动TShock进程");
                return null;
            }

            // 将进程添加到 Job Object 中（仅Windows）
            WindowsJobManager.AddProcess(process);

            // 等待一小段时间确保进程正常启动
            await Task.Delay(2000); // 增加等待时间以捕获更多潜在的启动输出

            if (process.HasExited)
            {
                var exitCode = process.ExitCode;
                _logger.LogError($"TShock进程启动后立即退出，服务器ID: {serverInfo.ServerId}, 退出代码: {exitCode}");

                // 异步读取所有输出以进行调试
                string output = await process.StandardOutput.ReadToEndAsync();
                string errorOutput = await process.StandardError.ReadToEndAsync();

                if (!string.IsNullOrWhiteSpace(output))
                {
                    _logger.LogWarning($"TShock进程标准输出:\n{output}");
                }
                if (!string.IsNullOrWhiteSpace(errorOutput))
                {
                    _logger.LogError($"TShock进程错误输出:\n{errorOutput}");
                }

                // 更新状态通知
                var failMessage = $"进程启动失败，退出代码: {exitCode}。详情请查看节点日志。";
                OnServerStatusUpdate?.Invoke(serverInfo.ServerId, "Error", failMessage);

                // 尝试将捕获的输出写入日志文件
                try
                {
                    using (var logWriter = new StreamWriter(serverInfo.LogPath, append: true))
                    {
                        await logWriter.WriteLineAsync($"\n--- TShock Process exited prematurely with code {exitCode} ---");
                        if (!string.IsNullOrWhiteSpace(output))
                        {
                            await logWriter.WriteLineAsync("--- Standard Output ---");
                            await logWriter.WriteLineAsync(output);
                        }
                        if (!string.IsNullOrWhiteSpace(errorOutput))
                        {
                            await logWriter.WriteLineAsync("--- Standard Error ---");
                            await logWriter.WriteLineAsync(errorOutput);
                        }
                        await logWriter.FlushAsync();
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"无法将启动失败信息写入日志文件: {serverInfo.LogPath}");
                }
                return null;
            }

            // 异步处理输出日志
            _ = Task.Run(() => ProcessOutputAsync(process, serverInfo.LogPath));

            serverInfo.ProcessId = process.Id;
            serverInfo.Process = process; // 保存进程引用
            serverInfo.Status = ServerStatus.Running;

            // 添加到服务器列表
            _servers[serverInfo.ServerId] = serverInfo;

            _logger.LogInformation($"TShock进程启动成功，PID: {process.Id}");
            var payload = new { Message = "启动成功", Port = serverInfo.Port };
            OnServerStatusUpdate?.Invoke(serverInfo.ServerId, "Online", JsonConvert.SerializeObject(payload));
            return process;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "启动TShock进程时发生错误");
            OnServerStatusUpdate?.Invoke(serverInfo.ServerId, "Error", $"启动进程时发生内部错误: {ex.Message}");
            return null;
        }
    }

    /// <summary>
    /// 构建TShock启动参数
    /// </summary>
    private string BuildTShockArguments(GameServerInfo serverInfo, StartServerCommand command)
    {
        var args = new List<string>
        {
            "-world", $"\"{Path.GetFullPath(serverInfo.MapPath)}\"",
            "-port", serverInfo.Port.ToString(),
            "-maxplayers", Math.Max(1, Math.Min(255, command.MaxPlayers)).ToString(), // 限制玩家数量范围
            "-autocreate", "1",
        };

        if (!string.IsNullOrWhiteSpace(command.Password))
        {
            args.AddRange(new[] { "-password", $"\"{command.Password}\"" });
        }

        // 添加世界名称参数
        if (!string.IsNullOrWhiteSpace(serverInfo.MapName))
        {
            args.AddRange(new[] { "-worldname", $"\"Boss 子世界: {serverInfo.MapName}\"" });
        }

        // 添加额外参数（验证安全性）
        if (command.AdditionalArgs != null)
        {
            foreach (var kvp in command.AdditionalArgs)
            {
                if (!string.IsNullOrWhiteSpace(kvp.Key) && !string.IsNullOrWhiteSpace(kvp.Value))
                {
                    // 过滤危险参数
                    if (IsSafeArgument(kvp.Key))
                    {
                        args.AddRange(new[] { kvp.Key, $"\"{kvp.Value}\"" });
                    }
                    else
                    {
                        _logger.LogWarning($"跳过不安全的参数: {kvp.Key}");
                    }
                }
            }
        }

        args.Add("-nolog");
        args.Add("-c");

        var argumentString = string.Join(" ", args);
        _logger.LogDebug($"TShock 启动参数: {argumentString}");
        return argumentString;
    }

    /// <summary>
    /// 检查参数是否安全
    /// </summary>
    private static bool IsSafeArgument(string argument)
    {
        // 定义允许的参数列表
        var allowedArgs = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "-lang", "-language", "-motd", "-seed", "-difficulty", "-worldsize"
        };

        return allowedArgs.Contains(argument);
    }

    /// <summary>
    /// 处理进程输出日志
    /// </summary>
    private async Task ProcessOutputAsync(Process process, string logPath)
    {
        StreamWriter? logWriter = null;
        try
        {
            // 确保日志目录存在
            var logDirectory = Path.GetDirectoryName(logPath);
            if (!string.IsNullOrEmpty(logDirectory) && !Directory.Exists(logDirectory))
            {
                Directory.CreateDirectory(logDirectory);
            }

            logWriter = new StreamWriter(logPath, append: true);

            while (!process.HasExited)
            {
                var output = await process.StandardOutput.ReadLineAsync();
                if (!string.IsNullOrEmpty(output))
                {
                    var logEntry = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {output}";
                    await logWriter.WriteLineAsync(logEntry);
                    await logWriter.FlushAsync();
                }

                // 同时处理错误输出
                if (process.StandardError.Peek() > -1)
                {
                    var error = await process.StandardError.ReadLineAsync();
                    if (!string.IsNullOrEmpty(error))
                    {
                        var errorEntry = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] ERROR: {error}";
                        await logWriter.WriteLineAsync(errorEntry);
                        await logWriter.FlushAsync();
                        _logger.LogWarning($"TShock进程错误输出: {error}");
                    }
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"处理进程输出日志时发生错误: {logPath}");
        }
        finally
        {
            logWriter?.Dispose();
        }
    }

    /// <summary>
    /// 准备日志路径
    /// </summary>
    private string PrepareLogPath(string serverId)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(serverId))
            {
                _logger.LogError("ServerId 不能为空");
                return string.Empty;
            }

            // 清理服务器ID中的非法字符
            var safeServerId = string.Join("_", serverId.Split(Path.GetInvalidFileNameChars()));
            var logFileName = $"{safeServerId}_{DateTime.Now:yyyyMMdd_HHmmss}.log";
            var logPath = Path.Combine(_settings.LogsDirectory, logFileName);

            // 确保日志目录存在且可写
            if (!Directory.Exists(_settings.LogsDirectory))
            {
                Directory.CreateDirectory(_settings.LogsDirectory);
            }

            // 测试文件是否可写
            try
            {
                using (var testFile = File.Create(logPath))
                {
                    // 文件创建成功，立即删除测试文件
                }
                File.Delete(logPath);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"日志文件不可写: {logPath}");
                return string.Empty;
            }

            return logPath;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"准备日志路径时发生错误: {serverId}");
            return string.Empty;
        }
    }

    /// <summary>
    /// 验证TShock程序路径
    /// </summary>
    private bool ValidateTShockPath()
    {
        if (string.IsNullOrWhiteSpace(_settings.TShockPath))
        {
            _logger.LogError("TShock 程序路径未配置");
            return false;
        }

        if (!File.Exists(_settings.TShockPath))
        {
            _logger.LogError($"TShock 程序不存在: {_settings.TShockPath}");
            return false;
        }

        try
        {
            var fileInfo = new FileInfo(_settings.TShockPath);
            if (fileInfo.Length == 0)
            {
                _logger.LogError($"TShock 程序文件为空: {_settings.TShockPath}");
                return false;
            }

            // 检查文件是否可执行（Windows下检查扩展名）
            var extension = Path.GetExtension(_settings.TShockPath).ToLowerInvariant();
            if (extension != ".exe")
            {
                _logger.LogWarning($"TShock 程序文件扩展名可能不正确: {extension}");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"验证TShock程序时发生错误: {_settings.TShockPath}");
            return false;
        }

        return true;
    }

    /// <summary>
    /// 验证配置和确保必要的目录存在
    /// </summary>
    private void ValidateConfigurationAndEnsureDirectories()
    {
        // 验证配置
        if (_settings == null)
        {
            throw new InvalidOperationException("ServerSettings 配置为空");
        }

        if (string.IsNullOrWhiteSpace(_settings.MapsDirectory))
        {
            throw new InvalidOperationException("MapsDirectory 配置为空");
        }

        if (string.IsNullOrWhiteSpace(_settings.LogsDirectory))
        {
            throw new InvalidOperationException("LogsDirectory 配置为空");
        }

        if (string.IsNullOrWhiteSpace(_settings.DefaultMapPath))
        {
            throw new InvalidOperationException("DefaultMapPath 配置为空");
        }

        if (_settings.BasePort <= 0 || _settings.BasePort > 65535)
        {
            throw new InvalidOperationException($"BasePort 配置无效: {_settings.BasePort}");
        }

        // 确保目录存在
        var directories = new[] { _settings.MapsDirectory, _settings.LogsDirectory };

        foreach (var directory in directories)
        {
            try
            {
                if (!Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                    _logger.LogInformation($"创建目录: {directory}");
                }

                // 测试目录是否可写
                var testFile = Path.Combine(directory, $"test_{Guid.NewGuid()}.tmp");
                File.WriteAllText(testFile, "test");
                File.Delete(testFile);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"目录创建或写入测试失败: {directory}");
                throw new InvalidOperationException($"无法创建或写入目录: {directory}", ex);
            }
        }

        // 验证默认地图路径
        if (!Directory.Exists(_settings.DefaultMapPath))
        {
            _logger.LogError($"默认地图目录不存在: {_settings.DefaultMapPath}");
            throw new InvalidOperationException($"默认地图目录不存在: {_settings.DefaultMapPath}");
        }

        // 检查默认地图文件是否存在
        var defaultMapFiles = Directory.GetFiles(_settings.DefaultMapPath, "*.wld");
        if (defaultMapFiles.Length == 0)
        {
            _logger.LogError($"默认地图目录中没有找到世界文件 (*.wld): {_settings.DefaultMapPath}");
            throw new InvalidOperationException($"默认地图目录中没有找到世界文件: {_settings.DefaultMapPath}");
        }

        _logger.LogInformation("配置验证和目录初始化完成");
    }
}