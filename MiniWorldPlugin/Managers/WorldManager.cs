using System.Collections.Concurrent;
using BossFramework;
using BossFramework.DB;
using MiniWorldPlugin.Services;
using MultiSCore.API;
using MultiSCore.Model;
using TShockAPI;
using MiniWorldModel = MiniWorld.Shared.Models.MiniWorld;
using ServerStatus = MiniWorld.Shared.Models.ServerStatus;
using StartServerCommand = MiniWorld.Shared.Models.StartServerCommand;
using StopServerCommand = MiniWorld.Shared.Models.StopServerCommand;
using WorldStatus = MiniWorld.Shared.Models.WorldStatus;

namespace MiniWorldPlugin.Managers
{
    /// <summary>
    /// 世界管理器 - 重构为基于 RPC 的架构
    /// </summary>
    public class WorldManager
    {
        private static readonly Lazy<WorldManager> _instance = new(() => new WorldManager());
        public static WorldManager Instance => _instance.Value;

        private readonly ConcurrentDictionary<int, MiniWorldModel> _worlds;
        private readonly ConcurrentDictionary<string, TSPlayer> _startingWorlds = new();
        private readonly ConcurrentDictionary<int, MiniWorldModel> _playerWorlds = new();
        private readonly System.Timers.Timer _emptyWorldCheckTimer;

        public WorldManager()
        {
            _worlds = new ConcurrentDictionary<int, MiniWorldModel>();

            // 初始化定时器，每10分钟检查一次无人世界
            _emptyWorldCheckTimer = new System.Timers.Timer(TimeSpan.FromMinutes(10).TotalMilliseconds);
            _emptyWorldCheckTimer.Elapsed += async (sender, e) => await CheckAndStopEmptyWorldsAsync();
            _emptyWorldCheckTimer.AutoReset = true;
        }

        /// <summary>
        /// 初始化世界管理器
        /// </summary>
        public async Task InitializeAsync()
        {
            var worldsFromDb = DBTools.SQL.Select<MiniWorldModel>().ToList();
            _worlds.Clear();
            foreach (var world in worldsFromDb)
            {
                // 初始化时，将所有世界状态设置为离线，后续由状态同步来确认真实状态
                world.Status = WorldStatus.Offline;
                world.NodeConnectionId = null;
                _worlds.TryAdd(world.Id, world);
            }
            TShock.Log.ConsoleInfo($"[MiniWorld] 从数据库加载了 {_worlds.Count} 个迷你世界数据。");

            // 如果RPC服务已连接，则立即同步一次状态
            if (RpcClientService.Instance.IsConnected)
            {
                await SyncServerStatusAsync();
                TShock.Log.ConsoleInfo($"[MiniWorld] 已同步服务器状态, 当前在线: {_worlds.Values.Count(w => w.Status == WorldStatus.Online)}");
            }

            // 启动定时器检查无人世界
            //_emptyWorldCheckTimer.Start();
            //TShock.Log.ConsoleInfo("[MiniWorld] 已启动无人世界检查定时器，每10分钟检查一次。");
        }

        /// <summary>
        /// 同步所有世界与节点服务器的真实状态
        /// </summary>
        public async Task SyncServerStatusAsync()
        {
            try
            {
                if (RpcClientService.Instance.NodeApi == null)
                {
                    TShock.Log.ConsoleWarn("[MiniWorld] RPC 客户端未连接，无法同步服务器状态。");
                    return;
                }

                var runningServers = await RpcClientService.Instance.NodeApi.GetServersInfoAsync();
                //TShock.Log.ConsoleInfo($"[MiniWorld] 从节点获取到 {runningServers.Count} 个运行中的服务器。");

                var runningServerIds = new HashSet<string>(runningServers.Select(s => s.ServerId));

                // 更新所有缓存中的世界状态
                foreach (var world in _worlds.Values)
                {
                    var serverInfo = runningServers.FirstOrDefault(s => s.ServerId == world.ServerId);
                    if (serverInfo != null) // 世界正在运行
                    {
                        UpdateWorldState(world, WorldStatus.Online, serverInfo.Port);
                    }
                    else // 世界不在运行
                    {
                        if (world.Status != WorldStatus.Offline)
                        {
                            UpdateWorldState(world, WorldStatus.Offline, 0);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                TShock.Log.ConsoleError($"[MiniWorld] 同步服务器状态失败: {ex}");
            }
        }

        /// <summary>
        /// 检查并关闭无人世界
        /// </summary>
        internal async Task<List<MiniWorldModel>> CheckAndStopEmptyWorldsAsync()
        {
            try
            {
                if (RpcClientService.Instance.NodeApi == null)
                {
                    //TShock.Log.ConsoleDebug("[MiniWorld] RPC 客户端未连接，跳过无人世界检查。");
                    return [];
                }
                await Instance.SyncServerStatusAsync();

                // 获取所有在线服务器状态
                var onlineSessions = MSCAPI.GetServerStatuses();

                var emptyWorlds = new List<MiniWorldModel>();

                // 查找玩家数量为0的世界
                foreach (var world in _worlds.Values.Where(w => w.Status is WorldStatus.Online))
                {
                    if (!onlineSessions.Exists(s => s.Server.Port == world.Port && s.PlayerCount > 0))
                    {
                        emptyWorlds.Add(world);
                    }
                }

                if (emptyWorlds.Count == 0)
                {
                    //TShock.Log.ConsoleDebug("[MiniWorld] 没有发现无人的迷你世界。");
                    return [];
                }

                TShock.Log.ConsoleInfo($"[MiniWorld] 发现 {emptyWorlds.Count} 个无人世界，准备关闭...");

                // 关闭无人世界
                foreach (var world in emptyWorlds)
                {
                    await StopWorldInternal(world);
                }
                return emptyWorlds;
            }
            catch (Exception ex)
            {
                TShock.Log.ConsoleError($"[MiniWorld] 检查无人世界时发生错误: {ex}");
                return [];
            }
        }

        /// <summary>
        /// 内部方法：关闭世界（不需要玩家权限检查）
        /// </summary>
        private async Task StopWorldInternal(MiniWorldModel world)
        {
            try
            {
                if (string.IsNullOrEmpty(world.ServerId))
                {
                    TShock.Log.ConsoleWarn($"[MiniWorld] 世界 '{world.WorldName}' (ID: {world.Id}) 状态异常（ServerId丢失），直接标记为离线。");
                    UpdateWorldState(world, WorldStatus.Offline, 0);
                    return;
                }

                var command = new StopServerCommand { ServerId = world.ServerId };
                var success = await RpcClientService.Instance.NodeApi.StopServerAsync(command);

                if (success)
                {
                    UpdateWorldState(world, WorldStatus.Offline, 0);
                    var owner = TShock.UserAccounts.GetUserAccountByID(world.OwnerId);
                    var ownerName = owner?.Name ?? "未知";
                    TShock.Log.ConsoleInfo($"[MiniWorld] 已自动关闭无人世界: {ownerName}/{world.WorldName} (ID: {world.Id})");
                }
                else
                {
                    TShock.Log.ConsoleWarn($"[MiniWorld] 关闭无人世界 '{world.WorldName}' (ID: {world.Id}) 失败，将同步状态。");
                    await SyncServerStatusAsync();
                }
            }
            catch (Exception ex)
            {
                TShock.Log.ConsoleError($"[MiniWorld] 关闭无人世界 '{world.WorldName}' (ID: {world.Id}) 时发生错误: {ex}");
            }
        }

        /// <summary>
        /// 处理节点断开连接的逻辑
        /// </summary>
        public void HandleNodeDisconnection(NodeManager nodeManager)
        {
            TShock.Log.ConsoleWarn("[MiniWorld] 节点已断开，正在清理所有相关世界状态...");

            // 遍历所有世界，将非离线状态的世界标记为离线
            foreach (var world in _worlds.Values)
            {
                if (world.Status != WorldStatus.Offline)
                {
                    TShock.Log.ConsoleInfo($"[MiniWorld] 世界 '{world.WorldName}' (ID: {world.Id}) 因节点断开而被标记为离线。");
                    UpdateWorldState(world, WorldStatus.Offline, 0);
                }
            }

            // 通知所有正在等待世界启动的玩家，启动失败
            if (!_startingWorlds.IsEmpty)
            {
                var waitingCount = _startingWorlds.Count;
                foreach (var entry in _startingWorlds)
                {
                    entry.Value.SendErrorMessage("启动世界的节点已断开连接，操作失败。");
                }
                _startingWorlds.Clear();
                TShock.Log.ConsoleWarn($"[MiniWorld] 清理了 {waitingCount} 个正在启动的世界请求。");
            }
        }

        public List<MiniWorldModel> GetAllWorlds()
        {
            return _worlds.Values.ToList();
        }

        public MiniWorldModel? GetWorld(int ownerId, string worldName)
        {
            return _worlds.Values.FirstOrDefault(w => w.OwnerId == ownerId && w.WorldName.Equals(worldName, StringComparison.OrdinalIgnoreCase));
        }

        public MiniWorldModel? GetWorldById(int worldId)
        {
            _worlds.TryGetValue(worldId, out var world);
            return world;
        }

        public List<MiniWorldModel> GetWorldsByOwner(int ownerId)
        {
            return _worlds.Values.Where(w => w.OwnerId == ownerId).ToList();
        }

        /// <summary>
        /// 前往指定世界
        /// </summary>
        public async Task GoToWorld(TSPlayer player, string worldName, string? ownerName = null)
        {
            if (player.Account == null)
            {
                player.SendErrorMessage("你必须登录才能执行此操作。");
                return;
            }

            MiniWorldModel? world;
            if (ownerName != null)
            {
                var owner = TShock.UserAccounts.GetUserAccountByName(ownerName);
                if (owner == null)
                {
                    player.SendErrorMessage($"找不到名为 '{ownerName}' 的玩家。");
                    return;
                }
                world = GetWorldsByOwner(owner.ID).FirstOrDefault(w => w.WorldName.Equals(worldName, StringComparison.OrdinalIgnoreCase));
            }
            else
            {
                world = GetWorld(player.Account.ID, worldName);
            }

            if (world == null)
            {
                player.SendErrorMessage($"找不到名为 '{worldName}' 的世界。");
                return;
            }

            if (!CanPlayerEnterWorld(player, world))
            {
                return;
            }

            await AttemptWorldEntryAsync(player, world);
        }

        /// <summary>
        /// 根据ID前往世界
        /// </summary>
        public async Task GoToWorldById(TSPlayer player, int worldId)
        {
            if (player.IsBeingForwarded())
            {
                player.SendWarningMessage($"你已处于迷你世界中或者正在进行传送, 请先使用 /mw back 返回主服务器或者等待传送完成.");
                return;
            }

            var world = GetWorldById(worldId);

            if (world == null)
            {
                player.SendErrorMessage($"找不到 ID 为 '{worldId}' 的世界。");
                return;
            }

            if (!CanPlayerEnterWorld(player, world))
            {
                return;
            }

            await AttemptWorldEntryAsync(player, world);
        }

        /// <summary>
        /// 尝试让玩家进入一个世界 - 检查实时状态
        /// </summary>
        private async Task AttemptWorldEntryAsync(TSPlayer player, MiniWorldModel world)
        {
            if (_startingWorlds.ContainsKey(world.ServerId ?? ""))
            {
                player.SendInfoMessage($"世界 '{world.WorldName}' 正在启动中，请稍候...");
                return;
            }
            if (MSCAPI.IsBeingForwarded(player))
            {
                player.SendWarningMessage($"你已处于迷你世界中, 请先使用 /mw back 返回主服务器");
                return;
            }

            try
            {
                if (RpcClientService.Instance.NodeApi == null)
                {
                    player.SendErrorMessage("与节点的连接已断开，无法操作世界。");
                    return;
                }

                // 如果 ServerId 为空，则世界从未启动过，直接视为离线
                if (string.IsNullOrEmpty(world.ServerId))
                {
                    await HandleOfflineWorldAsync(player, world);
                    return;
                }

                player.SendInfoMessage($"正在检查世界 '{world.WorldName}' 的实时状态...");
                var serverInfo = await RpcClientService.Instance.NodeApi.GetServerInfoAsync(world.ServerId);

                if (serverInfo != null && serverInfo.Status == ServerStatus.Running)
                {
                    await HandleOnlineWorldAsync(player, world, serverInfo);
                }
                else
                {
                    await HandleOfflineWorldAsync(player, world);
                }
            }
            catch (Exception ex)
            {
                TShock.Log.ConsoleError($"[MiniWorld] 尝试进入世界 '{world.WorldName}' 时发生错误: {ex}");
                player.SendErrorMessage("检查世界状态时发生错误，将尝试按离线流程启动世界...");
                await HandleOfflineWorldAsync(player, world); // 出现异常时，按离线逻辑处理
            }
        }

        /// <summary>
        /// 处理在线世界的进入逻辑
        /// </summary>
        private async Task HandleOnlineWorldAsync(TSPlayer player, MiniWorldModel world, MiniWorld.Shared.Models.GameServerInfo serverInfo)
        {
            UpdateWorldState(world, WorldStatus.Online, serverInfo.Port);
            player.SendSuccessMessage($"世界 '{world.WorldName}' 在线！正在传送...");
            await TeleportPlayerToWorld(player, world);
        }

        /// <summary>
        /// 处理离线世界的进入逻辑 - 启动世界并传送玩家
        /// </summary>
        private async Task HandleOfflineWorldAsync(TSPlayer player, MiniWorldModel world)
        {
            // 确保世界在尝试启动前状态为离线
            UpdateWorldState(world, WorldStatus.Offline, 0);

            if (!NodeManager.Instance.CanAcceptNewServer())
            {
                player.SendErrorMessage("当前没有可用的节点来启动你的世界，请稍后再试。");
                return;
            }

            // 准备启动，生成新的 ServerId
            var serverId = Guid.NewGuid().ToString("N");
            _startingWorlds[serverId] = player;

            try
            {
                // 更新状态为"启动中"
                UpdateWorldState(world, WorldStatus.Starting, 0, serverId);
                player.SendSuccessMessage($"世界 '{world.WorldName}' 当前离线，正在为你自动启动...");

                var command = new StartServerCommand
                {
                    ServerId = serverId,
                    ServerName = world.WorldName,
                    MapName = world.WorldName,
                    CreatorUserId = world.OwnerId,
                    MaxPlayers = 8 // 可配置
                };

                var serverInfo = await RpcClientService.Instance.NodeApi.StartServerAsync(command);

                if (serverInfo.Status == ServerStatus.Running)
                {
                    UpdateWorldState(world, WorldStatus.Online, serverInfo.Port);
                    player.SendSuccessMessage($"世界 '{world.WorldName}' 已启动, 请稍等几秒...");
                    await TeleportPlayerToWorld(player, world);
                }
                else
                {
                    UpdateWorldState(world, WorldStatus.Error);
                    player.SendErrorMessage($"启动世界 '{world.WorldName}' 失败。节点返回状态: {serverInfo.Status}");
                }
            }
            catch (Exception ex)
            {
                UpdateWorldState(world, WorldStatus.Error);
                player.SendErrorMessage("启动世界时发生严重错误，请联系管理员。");
                TShock.Log.ConsoleError($"[MiniWorld] 启动世界失败: {ex}");
            }
            finally
            {
                _startingWorlds.TryRemove(serverId, out _);
            }
        }

        /// <summary>
        /// 传送玩家到世界
        /// </summary>
        private async Task TeleportPlayerToWorld(TSPlayer player, MiniWorldModel world)
        {
            var port = world.Port > 0 ? world.Port : Config.Instance.DefaultWorldPortStart;
            var ip = new Uri(Config.Instance.RpcUrl).Host;

            // 使用 MultiSCore 的连接转发功能
            try
            {
                var forwardServer = new ServerInfo
                {
                    IP = ip,
                    Port = port,
                    Name = $"Boss 附属世界: {world.WorldName}",
                    Key = "Terraria279",
                    GlobalCommand = ["mw"]
                };

                BInfo.OnlinePlayers.Where(p => p.Name != player.Name).ForEach(p =>
                {
                    //p.SendInfoMsg($"[MiniWorld] {player.Name} 前往世界: {world.WorldName}");
                });

                await player.SwitchToServer(forwardServer, 30);


                AddPlayerWorld(player, world);
            }
            catch (Exception ex)
            {
                TShock.Log.ConsoleError($"[MiniWorld] 传送玩家失败: {ex}");
                player.SendErrorMessage("传送失败，请联系管理员");
            }
        }

        /// <summary>
        /// 创建新世界
        /// </summary>
        public async Task CreateWorld(TSPlayer player, string worldName)
        {
            if (player.Account == null)
            {
                player.SendErrorMessage("你必须登录才能创建世界。");
                return;
            }

            // 检查世界名称是否合法
            if (string.IsNullOrWhiteSpace(worldName) || worldName.Contains(" ") || worldName.Contains("/"))
            {
                player.SendErrorMessage("世界名称不能为空，且不能包含空格或'/'。");
                return;
            }

            // 检查玩家是否已拥有同名世界
            if (_worlds.Values.Any(w => w.OwnerId == player.Account.ID && w.WorldName.Equals(worldName, StringComparison.OrdinalIgnoreCase)))
            {
                player.SendErrorMessage($"你已经有一个名为 '{worldName}' 的世界了。");
                return;
            }

            // 检查世界是否已达到上限
            var worldCount = _worlds.Values.Count(w => w.OwnerId == player.Account.ID);
            if (worldCount >= Config.Instance.MaxWorldsPerPlayer)
            {
                player.SendErrorMessage($"你创建的世界已达到上限 ({Config.Instance.MaxWorldsPerPlayer}个)。");
                return;
            }

            player.SendInfoMessage($"正在为你创建世界 '{worldName}'...");

            // 在节点上创建地图文件
            var success = await RpcClientService.Instance.NodeApi.CreateMapAsync(player.Account.ID, worldName);
            if (success)
            {
                var newWorld = new MiniWorldModel
                {
                    Id = DBTools.SQL.Select<MiniWorldModel>().Max(w => w.Id) + 1,
                    OwnerId = player.Account.ID,
                    OwnerName = player.Account.Name,
                    WorldName = worldName,
                    Status = WorldStatus.Offline,
                    IsPublic = true, // 默认公开
                    Port = 0,
                    AllowedEditors = [], // 使用正确的字段名
                    CreateTime = DateTime.Now
                };

                try
                {
                    DBTools.SQL.Insert(newWorld).ExecuteAffrows();
                    _worlds.TryAdd(newWorld.Id, newWorld); // 假设Id在插入后会自动填充
                    player.SendSuccessMessage($"世界 '{worldName}' 创建成功！使用 /mw go {worldName} 进入。");
                }
                catch (Exception ex)
                {
                    player.SendErrorMessage($"在数据库中记录世界时出错: {ex.Message}");
                    TShock.Log.ConsoleError($"[MiniWorld] 创建世界 '{worldName}' (Owner: {player.Account.Name}) 插入数据库失败: {ex}");
                }
            }
            else
            {
                player.SendErrorMessage("在节点服务器上创建世界地图文件失败，请联系管理员。");
            }
        }

        /// <summary>
        /// 停止世界
        /// </summary>
        public async Task StopWorld(TSPlayer player, string worldName)
        {
            if (!TryGetWorldForManaging(player, worldName, out var world))
                return;

            if (world.Status != WorldStatus.Online)
            {
                player.SendErrorMessage($"世界 '{worldName}' 当前不在线。");
                return;
            }

            if (world.OwnerId != player.Account.ID && !player.HasPermission("mw.admin"))
            {
                player.SendErrorMessage("你没有权限停止这个世界。");
                return;
            }

            if (string.IsNullOrEmpty(world.ServerId))
            {
                player.SendErrorMessage("世界状态异常（ServerId丢失），无法停止。将尝试标记为离线。");
                UpdateWorldState(world, WorldStatus.Offline, 0);
                return;
            }

            try
            {
                if (RpcClientService.Instance.NodeApi == null)
                {
                    player.SendErrorMessage("与节点的连接已断开，无法停止世界。");
                    return;
                }

                player.SendInfoMessage($"正在停止世界 '{worldName}'...");

                var command = new StopServerCommand { ServerId = world.ServerId };
                var success = await RpcClientService.Instance.NodeApi.StopServerAsync(command);

                if (success)
                {
                    UpdateWorldState(world, WorldStatus.Offline, 0);
                    player.SendSuccessMessage($"世界 '{worldName}' 已成功停止。");
                }
                else
                {
                    // 即使停止失败，也警告并同步一次状态，以防节点已自行关闭
                    player.SendErrorMessage($"停止世界 '{worldName}' 的请求失败。将尝试与节点同步最新状态。");
                    await SyncServerStatusAsync();
                }
            }
            catch (Exception ex)
            {
                player.SendErrorMessage("停止世界时发生错误。");
                TShock.Log.ConsoleError($"[MiniWorld] 停止世界失败: {ex}");
            }
        }

        /// <summary>
        /// 设置世界公开状态
        /// </summary>
        public void SetPublicStatus(TSPlayer player, string worldName, bool isPublic)
        {
            if (!TryGetWorldForManaging(player, worldName, out var world))
                return;

            world.IsPublic = isPublic;
            DBTools.SQL.Update<MiniWorldModel>(world.Id)
                .Set(m => m.IsPublic, world.IsPublic)
                .ExecuteAffrows();

            var status = isPublic ? "公开" : "私有";
            player.SendSuccessMessage($"世界 '{world.WorldName}' 已设置为{status}。");
        }

        /// <summary>
        /// 添加编辑者
        /// </summary>
        public void AddEditor(TSPlayer player, string worldName, string editorName)
        {
            if (!TryGetWorldForManaging(player, worldName, out var world))
                return;

            var editorAccount = TShock.UserAccounts.GetUserAccountByName(editorName);
            if (editorAccount == null)
            {
                player.SendErrorMessage($"找不到名为 '{editorName}' 的玩家。");
                return;
            }

            if (world.AllowedEditors.Contains(editorAccount.ID))
            {
                player.SendErrorMessage($"'{editorName}' 已经是此世界的编辑者。");
                return;
            }

            world.AllowedEditors.Add(editorAccount.ID);
            DBTools.SQL.Update<MiniWorldModel>(world.Id)
                .Set(m => m.AllowedEditors, world.AllowedEditors)
                .ExecuteAffrows();

            player.SendSuccessMessage($"已将 '{editorName}' 添加为世界 '{worldName}' 的编辑者。");
        }

        /// <summary>
        /// 移除编辑者
        /// </summary>
        public void RemoveEditor(TSPlayer player, string worldName, string editorName)
        {
            if (!TryGetWorldForManaging(player, worldName, out var world))
                return;

            var editorAccount = TShock.UserAccounts.GetUserAccountByName(editorName);
            if (editorAccount == null)
            {
                player.SendErrorMessage($"找不到名为 '{editorName}' 的玩家。");
                return;
            }

            if (!world.AllowedEditors.Contains(editorAccount.ID))
            {
                player.SendErrorMessage($"'{editorName}' 不是此世界的编辑者。");
                return;
            }

            world.AllowedEditors.Remove(editorAccount.ID);
            DBTools.SQL.Update<MiniWorldModel>(world.Id)
                .Set(m => m.AllowedEditors, world.AllowedEditors)
                .ExecuteAffrows();

            player.SendSuccessMessage($"已将 '{editorName}' 从世界 '{worldName}' 的编辑者列表中移除。");
        }

        public MiniWorldModel GetPlayerWorld(TSPlayer player)
        {
            return _playerWorlds.TryGetValue(player.Index, out var world) ? world : null;
        }
        public (MiniWorldModel World, PlayerSession Session) GetPlayerWorldAndSession(TSPlayer player)
        {
            return (_playerWorlds.TryGetValue(player.Index, out var world) ? world : null, MSCAPI.GetPlayingSession(player));
        }
        public void AddPlayerWorld(TSPlayer player, MiniWorldModel world)
        {
            _playerWorlds[player.Index] = world;
        }
        public void RemovePlayerWorld(TSPlayer player)
        {
            _playerWorlds.TryRemove(player.Index, out _);
        }

        /// <summary>
        /// 尝试获取玩家可管理的世界
        /// </summary>
        private bool TryGetWorldForManaging(TSPlayer player, string worldName, out MiniWorldModel world)
        {
            world = null!;

            if (player.Account == null)
            {
                player.SendErrorMessage("你必须登录才能执行此操作。");
                return false;
            }

            world = GetWorld(player.Account.ID, worldName);
            if (world == null)
            {
                player.SendErrorMessage($"找不到你拥有的名为 '{worldName}' 的世界。");
                return false;
            }

            return true;
        }

        /// <summary>
        /// 检查玩家是否可以进入世界
        /// </summary>
        private bool CanPlayerEnterWorld(TSPlayer player, MiniWorldModel world)
        {
            if (player.Account == null)
            {
                player.SendErrorMessage("你必须登录才能进入世界。");
                return false;
            }
            if (player.HasPermission("mw.admin"))
                return true;

            // 世界所有者可以直接进入
            if (world.OwnerId == player.Account.ID)
                return true;

            // 公开世界任何人都可以进入
            if (world.IsPublic)
                return true;

            // 检查是否在编辑者列表中
            if (world.AllowedEditors.Contains(player.Account.ID))
                return true;

            player.SendErrorMessage($"你没有权限进入世界 '{world.WorldName}'。");
            return false;
        }

        /// <summary>
        /// 统一更新世界状态（内存和数据库）
        /// </summary>
        /// <param name="world">要更新的世界对象</param>
        /// <param name="status">新的状态</param>
        /// <param name="port">新的端口（如果适用）</param>
        /// <param name="serverId">新的服务器ID（如果适用）</param>
        private void UpdateWorldState(MiniWorldModel world, WorldStatus status, int port = 0, string? serverId = null)
        {
            var hasChanges = false;

            if (world.Status != status)
            {
                world.Status = status;
                hasChanges = true;
            }
            if (port != 0 && world.Port != port)
            {
                world.Port = port;
                hasChanges = true;
            }
            else if (status == WorldStatus.Offline || status == WorldStatus.Error)
            {
                if (world.Port != 0)
                {
                    world.Port = 0;
                    hasChanges = true;
                }
            }

            if (!string.IsNullOrEmpty(serverId) && world.ServerId != serverId)
            {
                world.ServerId = serverId;
                hasChanges = true;
            }

            if (hasChanges)
            {
                DBTools.SQL.Update<MiniWorldModel>(world.Id)
                    .Set(m => m.Status, world.Status)
                    .Set(m => m.Port, world.Port)
                    .Set(m => m.ServerId, world.ServerId)
                    .ExecuteAffrows();
            }
        }

        /// <summary>
        /// 释放资源
        /// </summary>
        public void Dispose()
        {
            _emptyWorldCheckTimer?.Stop();
            _emptyWorldCheckTimer?.Dispose();
            TShock.Log.ConsoleInfo("[MiniWorld] 无人世界检查定时器已停止。");
        }
    }
}