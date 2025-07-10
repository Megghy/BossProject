using System.Collections.Generic;
using System.Threading.Tasks;
using MiniWorld.Shared.Models;

namespace MiniWorld.Shared;

/// <summary>
/// 定义 MiniWorld 节点提供的 RPC 方法
/// </summary>
public interface IMiniWorldNodeApi
{
    /// <summary>
    /// 获取节点信息
    /// </summary>
    /// <returns>节点信息</returns>
    Task<NodeInfo> GetNodeInfoAsync();

    /// <summary>
    /// 获取所有游戏服务器的信息
    /// </summary>
    /// <returns>服务器信息列表</returns>
    Task<List<GameServerInfo>> GetServersInfoAsync();

    /// <summary>
    /// 获取指定ID的游戏服务器信息
    /// </summary>
    /// <param name="serverId">服务器ID</param>
    /// <returns>如果找到则返回服务器信息, 否则返回 null</returns>
    Task<GameServerInfo?> GetServerInfoAsync(string serverId);

    /// <summary>
    /// 启动一个新的游戏服务器
    /// </summary>
    /// <param name="command">启动参数</param>
    /// <returns>启动后的服务器信息</returns>
    Task<GameServerInfo> StartServerAsync(StartServerCommand command);

    /// <summary>
    /// 停止一个正在运行的游戏服务器
    /// </summary>
    /// <param name="command">停止参数</param>
    /// <returns>操作是否成功</returns>
    Task<bool> StopServerAsync(StopServerCommand command);

    /// <summary>
    /// 列出指定用户的所有地图
    /// </summary>
    /// <param name="creatorUserId">创建者ID</param>
    /// <returns>地图名称列表</returns>
    Task<string[]> ListMapsAsync(int creatorUserId);

    /// <summary>
    /// 创建一个新的地图
    /// </summary>
    /// <param name="creatorUserId">创建者ID</param>
    /// <param name="mapName">地图名称</param>
    /// <returns>操作是否成功</returns>
    Task<bool> CreateMapAsync(int creatorUserId, string mapName);

    /// <summary>
    /// 删除一个地图
    /// </summary>
    /// <param name="creatorUserId">创建者ID</param>
    /// <param name="mapName">地图名称</param>
    /// <returns>操作是否成功</returns>
    Task<bool> DeleteMapAsync(int creatorUserId, string mapName);

    /// <summary>
    /// 复制一个地图
    /// </summary>
    /// <param name="creatorUserId">创建者ID</param>
    /// <param name="sourceMapName">源地图名称</param>
    /// <param name="destMapName">目标地图名称</param>
    /// <returns>操作是否成功</returns>
    Task<bool> CopyMapAsync(int creatorUserId, string sourceMapName, string destMapName);
}