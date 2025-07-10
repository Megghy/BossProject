using System;
using System.Diagnostics;
using System.Text.Json.Serialization;

namespace MiniWorld.Shared.Models;

/// <summary>
/// 游戏服务器信息
/// </summary>
public class GameServerInfo
{
    /// <summary>
    /// 服务器ID
    /// </summary>
    public string ServerId { get; set; } = string.Empty;

    /// <summary>
    /// 服务器名称
    /// </summary>
    public string ServerName { get; set; } = string.Empty;

    /// <summary>
    /// 地图名称
    /// </summary>
    public string MapName { get; set; } = string.Empty;

    /// <summary>
    /// 端口号
    /// </summary>
    public int Port { get; set; }

    /// <summary>
    /// 服务器状态
    /// </summary>
    public ServerStatus Status { get; set; }

    /// <summary>
    /// 进程ID
    /// </summary>
    public int ProcessId { get; set; }

    /// <summary>
    /// 启动时间
    /// </summary>
    public DateTime StartTime { get; set; }

    /// <summary>
    /// 地图文件路径
    /// </summary>
    public string MapPath { get; set; } = string.Empty;

    /// <summary>
    /// 日志文件路径
    /// </summary>
    public string LogPath { get; set; } = string.Empty;

    /// <summary>
    /// 进程引用（内部使用，不参与序列化）
    /// </summary>
    [JsonIgnore]
    [Newtonsoft.Json.JsonIgnore]
    public Process? Process { get; set; }
}

/// <summary>
/// 服务器状态枚举
/// </summary>
public enum ServerStatus
{
    /// <summary>
    /// 已停止
    /// </summary>
    Stopped = 0,

    /// <summary>
    /// 启动中
    /// </summary>
    Starting = 1,

    /// <summary>
    /// 运行中
    /// </summary>
    Running = 2,

    /// <summary>
    /// 停止中
    /// </summary>
    Stopping = 3,

    /// <summary>
    /// 错误状态
    /// </summary>
    Error = 4
}