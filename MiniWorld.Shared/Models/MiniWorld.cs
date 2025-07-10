using System;
using System.Collections.Generic;

namespace MiniWorld.Shared.Models;

/// <summary>
/// 微型世界信息
/// </summary>
public class MiniWorld
{
    public int Id { get; set; }

    public int OwnerId { get; set; }

    public string OwnerName { get; set; } = "";

    public string WorldName { get; set; } = "";

    public Guid? NodeConnectionId { get; set; }

    public string? ServerId { get; set; }

    public int Port { get; set; }

    public WorldStatus Status { get; set; } = WorldStatus.Offline;

    public bool IsPublic { get; set; } = false;

    public List<int> AllowedEditors { get; set; } = new();

    public DateTime CreateTime { get; set; } = DateTime.Now;
}

public enum WorldStatus
{
    Offline,
    Starting,
    Online,
    Stopping,
    Error
}