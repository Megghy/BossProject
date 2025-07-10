using System.Net.WebSockets;
using MiniWorld.Shared;
using StreamJsonRpc;
using TShockAPI;

namespace MiniWorldPlugin.Services
{
    /// <summary>
    /// RPC 客户端服务，负责与 MiniWorldNode 通信
    /// </summary>
    public class RpcClientService : IDisposable
    {
        private static readonly Lazy<RpcClientService> _instance = new(() => new RpcClientService());
        public static RpcClientService Instance => _instance.Value;

        private ClientWebSocket? _webSocket;
        private JsonRpc? _rpc;
        private IMiniWorldNodeApi? _nodeApi;
        private bool _isConnected = false;
        private string? _rpcUrl;
        private TimeSpan _retryInterval;
        private Task? _reconnectTask;
        private CancellationTokenSource? _reconnectCts;

        public event EventHandler<bool>? ConnectionStatusChanged;

        /// <summary>
        /// 获取节点 API 代理对象
        /// </summary>
        public IMiniWorldNodeApi? NodeApi => _nodeApi;

        /// <summary>
        /// 是否已连接到节点
        /// </summary>
        public bool IsConnected => _isConnected;

        private RpcClientService()
        {
        }

        /// <summary>
        /// 连接到 RPC 服务器
        /// </summary>
        /// <param name="rpcUrl">RPC 服务器地址</param>
        public async Task<bool> ConnectAsync(string rpcUrl, bool log = true)
        {
            try
            {
                await DisconnectAsync();

                _webSocket = new ClientWebSocket();
                var uri = new UriBuilder(rpcUrl) { Scheme = "ws" }.Uri; // http -> ws

                await _webSocket.ConnectAsync(uri, CancellationToken.None);

                if (_webSocket.State == WebSocketState.Open)
                {
                    var messageHandler = new WebSocketMessageHandler(_webSocket);
                    _rpc = new JsonRpc(messageHandler);
                    _nodeApi = _rpc.Attach<IMiniWorldNodeApi>();

                    _rpc.Disconnected += OnRpcDisconnected;
                    _rpc.StartListening();

                    _isConnected = true;
                    ConnectionStatusChanged?.Invoke(this, true);

                    if (log)
                    {
                        TShock.Log.ConsoleInfo($"[MiniWorld] 已连接到节点 RPC 服务器: {rpcUrl}");
                    }
                    return true;
                }
                else
                {
                    if (log)
                    {
                        TShock.Log.ConsoleError($"[MiniWorld] 连接到 WebSocket 服务器失败: {_webSocket.State}");
                    }
                    await DisconnectAsync();
                    return false;
                }
            }
            catch (Exception ex)
            {
                if (log)
                {
                    TShock.Log.ConsoleError($"[MiniWorld] 连接到 RPC 服务器时出错: {ex.Message}");
                }
                await DisconnectAsync();
                return false;
            }
        }

        /// <summary>
        /// 断开连接
        /// </summary>
        public async Task DisconnectAsync()
        {
            if (_rpc != null)
            {
                _rpc.Disconnected -= OnRpcDisconnected;

                try
                {
                    if (!_rpc.IsDisposed)
                    {
                        _rpc.Dispose();
                    }
                    await _rpc.Completion;
                }
                catch
                {
                    // 忽略断开连接时的异常
                }
                _rpc = null;
            }

            if (_webSocket != null)
            {
                if (_webSocket.State == WebSocketState.Open || _webSocket.State == WebSocketState.Connecting)
                {
                    await _webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "客户端主动断开连接", CancellationToken.None);
                }
                _webSocket.Dispose();
                _webSocket = null;
            }
            _nodeApi = null;

            if (_isConnected)
            {
                _isConnected = false;
                ConnectionStatusChanged?.Invoke(this, false);
                TShock.Log.ConsoleInfo("[MiniWorld] 已断开与节点的连接");
            }
        }

        /// <summary>
        /// 启动自动重连
        /// </summary>
        /// <param name="rpcUrl">RPC 服务器地址</param>
        /// <param name="retryInterval">重试间隔</param>
        public void StartAutoReconnect(string rpcUrl, TimeSpan retryInterval)
        {
            _rpcUrl = rpcUrl;
            _retryInterval = retryInterval;

            // 如果已有重连任务在运行，先停止
            StopAutoReconnect();

            _reconnectCts = new CancellationTokenSource();
            _reconnectTask = Task.Run(() => AutoReconnectLoop(_reconnectCts.Token), _reconnectCts.Token);
            TShock.Log.ConsoleInfo("[MiniWorld] 自动重连服务已启动");
        }

        /// <summary>
        /// 停止自动重连
        /// </summary>
        public void StopAutoReconnect()
        {
            if (_reconnectCts != null && !_reconnectCts.IsCancellationRequested)
            {
                _reconnectCts.Cancel();
                _reconnectTask?.Wait();
            }
            _reconnectCts?.Dispose();
            _reconnectCts = null;
            _reconnectTask = null;
        }

        private async Task AutoReconnectLoop(CancellationToken cancellationToken)
        {
            TShock.Log.ConsoleInfo("[MiniWorld] 自动重连循环已启动");
            while (!cancellationToken.IsCancellationRequested)
            {
                if (!_isConnected)
                {
                    try
                    {
                        //TShock.Log.ConsoleInfo($"[MiniWorld] 正在尝试连接到节点: {_rpcUrl}...");
                        await ConnectAsync(_rpcUrl!, false);
                    }
                    catch (Exception ex)
                    {
                        // 记录连接尝试期间的任何异常
                        TShock.Log.ConsoleError($"[MiniWorld] 自动重连失败: {ex.Message}");
                    }
                }

                try
                {
                    await Task.Delay(_retryInterval, cancellationToken);
                }
                catch (TaskCanceledException)
                {
                    // 任务被取消时，跳出循环
                    break;
                }
            }
            TShock.Log.ConsoleInfo("[MiniWorld] 自动重连循环已停止");
        }

        private void OnRpcDisconnected(object? sender, JsonRpcDisconnectedEventArgs e)
        {
            _isConnected = false;
            ConnectionStatusChanged?.Invoke(this, false);
            TShock.Log.ConsoleWarn($"[MiniWorld] 与节点的连接已断开: {e.Reason}");
        }

        public void Dispose()
        {
            StopAutoReconnect();
            _ = DisconnectAsync();
        }
    }
}