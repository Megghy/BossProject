using System.Collections.Concurrent;
using MultiSCore.Model;
using TShockAPI;

namespace MultiSCore.Services
{
    /// <summary>
    /// 管理所有玩家的会话状态，包括跨服会话和被转发的玩家信息。
    /// 线程安全。
    /// </summary>
    public class SessionManager
    {
        public static SessionManager Instance { get; private set; }
        private readonly Config _config;
        private readonly Version _pluginVersion;
        // 存储正在跨服的玩家会话 (key: Player Index)
        private readonly ConcurrentDictionary<int, PlayerSession> _sessions = new();

        // 存储从其他服务器转发过来的玩家信息 (key: Player Index)
        private readonly ConcurrentDictionary<int, ForwardedPlayerInfo> _forwardedPlayers = new();

        // 存储直连玩家的Terraria版本号 (key: Player Index)
        private readonly ConcurrentDictionary<int, string> _directPlayerVersions = new();

        public SessionManager(Config config, Version pluginVersion)
        {
            _config = config;
            _pluginVersion = pluginVersion;
            Instance = this;
        }

        #region Session Management

        /// <summary>
        /// 为一个玩家创建一个新的跨服会话。
        /// </summary>
        public PlayerSession CreateSession(TSPlayer player, int timeout = 30)
        {
            var version = GetPlayerTerrariaVersion(player.Index);
            var session = new PlayerSession(player.Index, version, _config, _pluginVersion, timeout);
            _sessions[player.Index] = session;
            return session;
        }

        /// <summary>
        /// 获取一个玩家的跨服会话。
        /// </summary>
        public PlayerSession GetSession(int playerIndex)
        {
            _sessions.TryGetValue(playerIndex, out var session);
            return session;
        }

        /// <summary>
        /// 尝试获取一个玩家的跨服会话。
        /// </summary>
        /// <param name="playerIndex">玩家索引</param>
        /// <param name="session">找到的会话，如果没有则为 null</param>
        /// <returns>如果找到会话则返回 true，否则返回 false</returns>
        public bool TryGetSession(int playerIndex, out PlayerSession session)
        {
            return _sessions.TryGetValue(playerIndex, out session);
        }

        /// <summary>
        /// 检查一个玩家当前是否正在进行跨服。
        /// </summary>
        /// <param name="playerIndex">玩家索引</param>
        /// <returns>如果是，则返回 true</returns>
        public bool IsPlayerForwarded(int playerIndex)
        {
            return _sessions.ContainsKey(playerIndex);
        }

        /// <summary>
        /// 移除并清理一个玩家的跨服会话。
        /// </summary>
        public void RemoveSession(int playerIndex)
        {
            if (_sessions.TryRemove(playerIndex, out var session))
            {
                session.Dispose();
            }
            _forwardedPlayers.TryRemove(playerIndex, out _);
            _directPlayerVersions.TryRemove(playerIndex, out _);
        }

        #endregion

        #region Forwarded Player Management

        /// <summary>
        /// 将一个玩家切换到指定的子服务器。
        /// </summary>
        /// <param name="player">要切换的玩家</param>
        /// <param name="server">目标服务器信息</param>
        public async Task<PlayerSession> SwitchPlayerToServer(TSPlayer player, ServerInfo server, int timeout = 10)
        {
            if (IsPlayerForwarded(player.Index))
            {
                player.SendErrorMessage("你已经在切换服务器的过程中了。");
                return GetSession(player.Index);
            }

            var session = CreateSession(player, timeout);
            await session.SwitchServerAsync(server);
            return session;
        }

        /// <summary>
        /// 注册一个从其他服务器转发来的玩家。
        /// </summary>
        public void AddForwardedPlayer(int playerIndex, ForwardedPlayerInfo info)
        {
            _forwardedPlayers[playerIndex] = info;
        }

        /// <summary>
        /// 获取一个被转发玩家的来源信息。
        /// </summary>
        public ForwardedPlayerInfo GetForwardedPlayerInfo(int playerIndex)
        {
            _forwardedPlayers.TryGetValue(playerIndex, out var info);
            return info;
        }


        #endregion

        #region Direct Player Version Management

        /// <summary>
        /// 记录一个直连玩家的Terraria版本号。
        /// </summary>
        public void SetPlayerTerrariaVersion(int playerIndex, string version)
        {
            _directPlayerVersions[playerIndex] = version;
        }

        /// <summary>
        /// 获取玩家的Terraria版本号。
        /// </summary>
        public string GetPlayerTerrariaVersion(int playerIndex)
        {
            _directPlayerVersions.TryGetValue(playerIndex, out var version);
            return version ?? "Unknown";
        }

        #endregion

        /// <summary>
        /// 当玩家离开时，清理所有相关的会话和信息。
        /// </summary>
        public void OnPlayerLeave(int playerIndex)
        {
            RemoveSession(playerIndex);
        }

        /// <summary>
        /// 获取所有活动子服务器的状态，包括玩家数量。
        /// </summary>
        /// <returns>一个包含服务器状态信息的列表。</returns>
        public List<ServerStatusInfo> GetServerStatuses()
        {
            return [.. _sessions.Values
        .Where(s => s.State > SessionState.Connecting)
        .GroupBy(s => s.TargetServer.Port) // 使用服务器名称（即ServerId）进行分组
        .Select(g => new ServerStatusInfo
        {
          Server = g.First().TargetServer, // 同组的TargetServer是相同的
          PlayerCount = g.Count()
        })];
        }
    }
}