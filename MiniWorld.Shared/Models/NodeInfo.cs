using System;

namespace MiniWorld.Shared.Models;

/// <summary>
/// 节点信息
/// </summary>
public class NodeInfo
{
    public string? IPAddress { get; set; }

    /// <summary>
    /// 节点ID
    /// </summary>
    public string NodeId { get; set; } = string.Empty;

    /// <summary>
    /// 节点名称
    /// </summary>
    public string NodeName { get; set; } = string.Empty;

    /// <summary>
    /// 运行中的服务器数量
    /// </summary>
    public int RunningServers { get; set; }

    /// <summary>
    /// 最大服务器数量
    /// </summary>
    public int MaxServers { get; set; } = 10;

    /// <summary>
    /// CPU使用率
    /// </summary>
    public double CpuUsage { get; set; }

    /// <summary>
    /// 内存使用率
    /// </summary>
    public double MemoryUsage { get; set; }

    /// <summary>
    /// 节点状态
    /// </summary>
    public NodeStatus Status { get; set; }

    /// <summary>
    /// 最后更新时间
    /// </summary>
    public DateTime LastUpdate { get; set; }
}

/// <summary>
/// 节点状态枚举
/// </summary>
public enum NodeStatus
{
    /// <summary>
    /// 离线
    /// </summary>
    Offline = 0,

    /// <summary>
    /// 在线
    /// </summary>
    Online = 1,

    /// <summary>
    /// 忙碌
    /// </summary>
    Busy = 2,

    /// <summary>
    /// 维护中
    /// </summary>
    Maintenance = 3
}