using System.Diagnostics;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MiniWorld.Shared.Models;
using MiniWorldNode.Models;

namespace MiniWorldNode.Services;

/// <summary>
/// 节点信息服务
/// </summary>
public class NodeInfoService
{
    private readonly ILogger<NodeInfoService> _logger;
    private readonly ServerSettings _settings;
    private readonly GameServerManager _serverManager;
    //private readonly PerformanceCounter? _cpuCounter;
    //private readonly PerformanceCounter? _memoryCounter;

    public NodeInfoService(
        ILogger<NodeInfoService> logger,
        IOptions<ServerSettings> settings,
        GameServerManager serverManager)
    {
        _logger = logger;
        _settings = settings.Value;
        _serverManager = serverManager;

        try
        {
            // 初始化性能计数器
            //_cpuCounter = new PerformanceCounter("Processor", "% Processor Time", "_Total");
            //_memoryCounter = new PerformanceCounter("Memory", "Available MBytes");
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "无法初始化性能计数器，将使用替代方法获取系统信息");
        }
    }

    /// <summary>
    /// 获取节点信息
    /// </summary>
    public async Task<NodeInfo> GetNodeInfoAsync()
    {
        try
        {
            var runningServers = _serverManager.GetAllServers().Count(s => s.Status == ServerStatus.Running);

            var nodeInfo = new NodeInfo
            {
                NodeId = _settings.NodeId,
                NodeName = _settings.NodeName,
                RunningServers = runningServers,
                MaxServers = 10, // 可以从配置中读取
                CpuUsage = await GetCpuUsageAsync(),
                MemoryUsage = await GetMemoryUsageAsync(),
                Status = DetermineNodeStatus(runningServers),
                LastUpdate = DateTime.Now
            };

            return nodeInfo;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取节点信息时发生错误");

            return new NodeInfo
            {
                NodeId = _settings.NodeId,
                NodeName = _settings.NodeName,
                Status = NodeStatus.Offline,
                LastUpdate = DateTime.Now
            };
        }
    }

    /// <summary>
    /// 获取CPU使用率
    /// </summary>
    private async Task<double> GetCpuUsageAsync()
    {
        try
        {
            /*if (_cpuCounter != null)
            {
                // 使用性能计数器
                var cpuUsage = _cpuCounter.NextValue();

                // 第一次调用通常返回0，需要等待后再次调用
                if (cpuUsage == 0)
                {
                    await Task.Delay(100);
                    cpuUsage = _cpuCounter.NextValue();
                }

                return Math.Round(cpuUsage, 2);
            }
            else*/
            {
                // 使用Process类的替代方法
                using var process = Process.GetCurrentProcess();
                var startTime = DateTime.UtcNow;
                var startCpuUsage = process.TotalProcessorTime;

                await Task.Delay(500);

                var endTime = DateTime.UtcNow;
                var endCpuUsage = process.TotalProcessorTime;

                var cpuUsedMs = (endCpuUsage - startCpuUsage).TotalMilliseconds;
                var totalMsPassed = (endTime - startTime).TotalMilliseconds;
                var cpuUsageTotal = cpuUsedMs / (Environment.ProcessorCount * totalMsPassed);

                return Math.Round(cpuUsageTotal * 100, 2);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取CPU使用率时发生错误");
            return 0;
        }
    }

    /// <summary>
    /// 获取内存使用率
    /// </summary>
    private async Task<double> GetMemoryUsageAsync()
    {
        try
        {
            var gc = GC.GetTotalMemory(false);
            var workingSet = Environment.WorkingSet;

            /*if (_memoryCounter != null)
            {
                var availableMemoryMB = _memoryCounter.NextValue();
                var totalMemoryMB = GetTotalPhysicalMemory() / (1024 * 1024);
                var usedMemoryMB = totalMemoryMB - availableMemoryMB;
                var memoryUsage = (usedMemoryMB / totalMemoryMB) * 100;

                return Math.Round(memoryUsage, 2);
            }
            else*/
            {
                // 使用GC和WorkingSet作为替代
                var memoryUsage = (double)workingSet / (1024 * 1024 * 1024); // 转换为GB
                return Math.Round(memoryUsage, 2);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取内存使用率时发生错误");
            return 0;
        }
    }

    /// <summary>
    /// 获取总物理内存（字节）
    /// </summary>
    private static long GetTotalPhysicalMemory()
    {
        try
        {
            if (OperatingSystem.IsWindows())
            {
                using var searcher = new System.Management.ManagementObjectSearcher("SELECT TotalPhysicalMemory FROM Win32_ComputerSystem");
                foreach (var obj in searcher.Get())
                {
                    return Convert.ToInt64(obj["TotalPhysicalMemory"]);
                }
            }

            // 如果无法获取，返回默认值 (8GB)
            return 8L * 1024 * 1024 * 1024;
        }
        catch
        {
            return 8L * 1024 * 1024 * 1024;
        }
    }

    /// <summary>
    /// 确定节点状态
    /// </summary>
    private NodeStatus DetermineNodeStatus(int runningServers)
    {
        if (runningServers == 0)
        {
            return NodeStatus.Online;
        }
        else if (runningServers >= 8) // 接近最大容量
        {
            return NodeStatus.Busy;
        }
        else
        {
            return NodeStatus.Online;
        }
    }

    /// <summary>
    /// 释放资源
    /// </summary>
    public void Dispose()
    {
        //_cpuCounter?.Dispose();
        //_memoryCounter?.Dispose();
    }
}