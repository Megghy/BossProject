using MultiSCore.Common;
using MultiSCore.Model;
using MultiSCore.Services;
using TShockAPI;

namespace MultiSCore.API
{
    /// <summary>
    /// 为 MultiSCore 功能提供一套公共 API。
    /// </summary>
    public static class MSCAPI
    {
        private static SessionManager _sessionManager;
        private static Config _config;

        /// <summary>
        /// 初始化 MSCAPI，由 MSCPlugin 在启动时调用。
        /// </summary>
        /// <param name="sessionManager">SessionManager 实例</param>
        /// <param name="config">Config 实例</param>
        internal static void Initialize(SessionManager sessionManager, Config config)
        {
            _sessionManager = sessionManager;
            _config = config;
        }

        /// <summary>
        /// 检查玩家当前是否正被代理到其他服务器。
        /// </summary>
        /// <param name="player">玩家实例</param>
        /// <returns>如果是，则返回 true。</returns>
        public static bool IsBeingForwarded(this TSPlayer player)
        {
            return player != null && _sessionManager.IsPlayerForwarded(player.Index);
        }

        /// <summary>
        /// 将玩家切换到指定名称的子服务器。
        /// </summary>
        /// <param name="player">玩家实例</param>
        /// <param name="serverName">目标服务器在配置文件中的名称</param>
        /// <returns>如果成功发起切换，则返回 true；否则返回 false。</returns>
        public static async Task<bool> SwitchToServer(this TSPlayer player, string serverName, int timeout = 10)
        {
            var server = _config.Servers.FirstOrDefault(s => s.Name.Equals(serverName, StringComparison.OrdinalIgnoreCase));
            if (server == null)
            {
                player.SendErrorMsg($"找不到名为 '{serverName}' 的服务器。");
                return false;
            }

            return await SwitchToServer(player, server, timeout);
        }

        /// <summary>
        /// 将玩家切换到指定的子服务器。
        /// </summary>
        /// <param name="player">玩家实例</param>
        /// <param name="server">目标服务器配置</param>
        /// <returns>如果成功发起切换，则返回 true；否则返回 false。</returns>
        public static async Task<bool> SwitchToServer(this TSPlayer player, ServerInfo server, int timeout = 10)
        {
            if (player == null || !player.Active)
            {
                return false;
            }

            if (IsBeingForwarded(player))
            {
                player.SendErrorMsg("你已处于服务器切换中，无法重复操作。");
                return false;
            }

            if (server == null)
            {
                player.SendErrorMsg("目标服务器配置无效。");
                return false;
            }

            try
            {
                var session = await _sessionManager.SwitchPlayerToServer(player, server, timeout);
                player.SendInfoMsg($"正在将你切换到 {server.Name}...");
                return true;
            }
            catch (Exception ex)
            {
                TShock.Log.ConsoleError($"[MSCAPI] 切换服务器时发生错误: {ex}");
                player.SendErrorMsg("切换服务器时发生未知错误。");
                return false;
            }
        }

        /// <summary>
        /// 将玩家切回主服务器。
        /// </summary>
        /// <param name="player">玩家实例</param>
        /// <returns>如果成功发起切换，则返回 true；否则返回 false。</returns>
        public static bool BackToHostServer(this TSPlayer player)
        {
            if (player == null || !player.Active)
            {
                return false;
            }

            if (!IsBeingForwarded(player))
            {
                player.SendErrorMsg("你当前不在子服务器，无需返回。");
                return false;
            }

            try
            {
                if (_sessionManager.TryGetSession(player.Index, out var session))
                {
                    session.ReturnToHost();
                    return true;
                }
                else
                {
                    // 理论上 IsBeingForwarded() 为 true 时，session 不应为 null
                    // 但作为安全措施，处理此情况
                    player.SendErrorMsg("找不到你的服务器代理信息，无法返回。");
                    TShock.Log.Warn($"[MSCAPI] 玩家 {player.Name} 处于 Forward 状态，但找不到 PlayerSession 实例。");
                    return false;
                }
            }
            catch (Exception ex)
            {
                TShock.Log.ConsoleError($"[MSCAPI] 返回主服务器时发生错误: {ex}");
                player.SendErrorMsg("返回主服务器时发生未知错误。");
                return false;
            }
        }

        /// <summary>
        /// 获取所有活动子服务器的状态信息，包括玩家数量。
        /// </summary>
        /// <returns>服务器状态信息列表。</returns>
        public static List<ServerStatusInfo> GetServerStatuses()
        {
            return _sessionManager?.GetServerStatuses() ?? new List<ServerStatusInfo>();
        }
        public static PlayerSession? GetPlayingSession(this TSPlayer plr)
        {
            return _sessionManager.GetSession(plr.Index);
        }
    }
}