using System.Collections.Generic;

namespace MiniWorld.Shared.Models;

/// <summary>
/// 启动服务器命令
/// </summary>
public class StartServerCommand
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
    /// 创建者用户ID
    /// </summary>
    public int CreatorUserId { get; set; }

    /// <summary>
    /// 最大玩家数量
    /// </summary>
    public int MaxPlayers { get; set; } = 8;

    /// <summary>
    /// 服务器密码
    /// </summary>
    public string Password { get; set; } = string.Empty;

    /// <summary>
    /// 额外参数
    /// </summary>
    public Dictionary<string, string> AdditionalArgs { get; set; } = new();
}

/// <summary>
/// 停止服务器命令
/// </summary>
public class StopServerCommand
{
    /// <summary>
    /// 服务器ID
    /// </summary>
    public string ServerId { get; set; } = string.Empty;

    /// <summary>
    /// 是否强制停止
    /// </summary>
    public bool ForceStop { get; set; } = false;
}