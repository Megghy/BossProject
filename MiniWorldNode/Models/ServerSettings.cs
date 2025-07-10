namespace MiniWorldNode.Models;

/// <summary>
/// 服务器配置设置
/// </summary>
public class ServerSettings
{
    /// <summary>
    /// RPC 服务器端口
    /// </summary>
    public int? RpcPort { get; set; } = 8080;

    /// <summary>
    /// 当前节点ID
    /// </summary>
    public string NodeId { get; set; } = string.Empty;

    /// <summary>
    /// 当前节点名称
    /// </summary>
    public string NodeName { get; set; } = string.Empty;

    /// <summary>
    /// 默认地图路径
    /// </summary>
    public string DefaultMapPath { get; set; } = string.Empty;

    /// <summary>
    /// 地图存储目录
    /// </summary>
    public string MapsDirectory { get; set; } = string.Empty;

    /// <summary>
    /// 日志存储目录
    /// </summary>
    public string LogsDirectory { get; set; } = string.Empty;

    /// <summary>
    /// TShock服务器程序路径
    /// </summary>
    public string TShockPath { get; set; } = string.Empty;

    /// <summary>
    /// 基础端口号
    /// </summary>
    public int BasePort { get; set; } = 7777;

    /// <summary>
    /// 最大重试次数
    /// </summary>
    public int MaxRetryAttempts { get; set; } = 3;

    /// <summary>
    /// 重试延迟时间（秒）
    /// </summary>
    public int RetryDelaySeconds { get; set; } = 5;
}