using MiniWorld.Shared.Models;
using MiniWorldPlugin.Services;
using TShockAPI;

namespace MiniWorldPlugin.Managers
{
    /// <summary>
    /// 节点管理器 - 简化版，只管理一个工作节点
    /// </summary>
    public class NodeManager : IDisposable
    {
        private static readonly Lazy<NodeManager> _instance = new(() => new NodeManager());
        public static NodeManager Instance => _instance.Value;

        private NodeInfo? _nodeInfo;
        private DateTime _lastHeartbeat = DateTime.MinValue;
        private readonly Timer _healthCheckTimer;
        private bool _isNodeAlive = true;

        public event EventHandler<bool>? NodeConnectionChanged;

        /// <summary>
        /// 当前节点信息
        /// </summary>
        public NodeInfo? CurrentNode => _nodeInfo;

        /// <summary>
        /// 节点是否在线
        /// </summary>
        public bool IsNodeOnline => _nodeInfo?.Status == NodeStatus.Online && RpcClientService.Instance.IsConnected;

        private NodeManager()
        {
            // 每30秒检查一次节点健康状态
            _healthCheckTimer = new Timer(CheckNodeHealth, null, TimeSpan.FromSeconds(30), TimeSpan.FromSeconds(30));

            // 监听 RPC 连接状态变化
            RpcClientService.Instance.ConnectionStatusChanged += OnRpcConnectionChanged;
        }

        public void Dispose()
        {
            _healthCheckTimer?.Dispose();
            RpcClientService.Instance.ConnectionStatusChanged -= OnRpcConnectionChanged;
        }

        /// <summary>
        /// 更新节点信息
        /// </summary>
        /// <param name="nodeInfo">节点信息</param>
        public async Task UpdateNodeInfoAsync()
        {
            try
            {
                if (RpcClientService.Instance.NodeApi != null)
                {
                    var nodeInfo = await RpcClientService.Instance.NodeApi.GetNodeInfoAsync();
                    _nodeInfo = nodeInfo;
                    _lastHeartbeat = DateTime.UtcNow;

                    //TShock.Log.ConsoleInfo($"[MiniWorld] 节点信息已更新: {nodeInfo.NodeName} ({nodeInfo.RunningServers}/{nodeInfo.MaxServers})");
                }
            }
            catch (Exception ex)
            {
                TShock.Log.ConsoleError($"[MiniWorld] 获取节点信息失败: {ex.Message}");
                _nodeInfo = null;
            }
        }

        /// <summary>
        /// 检查节点是否可用于新的服务器
        /// </summary>
        /// <returns>是否可用</returns>
        public bool CanAcceptNewServer()
        {
            if (_nodeInfo == null || !IsNodeOnline)
                return false;

            return _nodeInfo.RunningServers < _nodeInfo.MaxServers;
        }

        /// <summary>
        /// 获取节点状态摘要
        /// </summary>
        /// <returns>状态摘要</returns>
        public string GetNodeStatusSummary()
        {
            if (_nodeInfo == null)
                return "节点: 离线";

            var connectionStatus = RpcClientService.Instance.IsConnected ? "已连接" : "断开连接";
            return $"节点: {_nodeInfo.NodeName} ({connectionStatus}) - {_nodeInfo.RunningServers}/{_nodeInfo.MaxServers} 服务器";
        }

        private async void CheckNodeHealth(object? state)
        {
            if (RpcClientService.Instance.IsConnected)
            {
                await UpdateNodeInfoAsync();
                await WorldManager.Instance.SyncServerStatusAsync();
            }

            // 检查心跳超时
            var timeSinceLastHeartbeat = DateTime.UtcNow - _lastHeartbeat;
            if (timeSinceLastHeartbeat > TimeSpan.FromMinutes(2) && _nodeInfo != null)
            {
                if (!_isNodeAlive)
                {
                    _isNodeAlive = true;
                    TShock.Log.ConsoleWarn("[MiniWorld] 节点心跳超时，标记为离线");
                }
                _nodeInfo = null;
                NodeConnectionChanged?.Invoke(this, false);
            }
            _lastHeartbeat = DateTime.UtcNow;
        }

        private void OnRpcConnectionChanged(object? sender, bool isConnected)
        {
            if (isConnected)
            {
                _isNodeAlive = true;
                TShock.Log.ConsoleInfo("[MiniWorld] RPC 连接已建立，获取节点信息...");
                _ = UpdateNodeInfoAsync();
            }
            else
            {
                _isNodeAlive = false;
                TShock.Log.ConsoleWarn("[MiniWorld] RPC 连接已断开");
                // 清理节点状态
                WorldManager.Instance.HandleNodeDisconnection(this);
                _nodeInfo = null;
            }

            NodeConnectionChanged?.Invoke(this, isConnected);
        }
    }
}