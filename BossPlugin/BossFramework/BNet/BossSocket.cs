using System.Net;
using Terraria;
using Terraria.Net;
using Terraria.Net.Sockets;

namespace BossFramework.BNet
{
    /// <summary>
    /// ISocket 接口的适配器实现，用于将 NetCoreServer 集成到 Terraria 的网络层。
    /// 这个类具有双重角色：
    /// 1. 服务端角色：通过静态方法和实例管理监听套接字 (Listener)。
    /// 2. 客户端角色：每个实例代表一个已连接的客户端。
    /// </summary>
    public class BossSocket : ISocket
    {
        #region 事件
        /// <summary>
        /// 在即将发送数据包时触发。
        /// 订阅此事件可以监视或取消传出的数据包。
        /// </summary>
        public static event EventHandler<PacketSendingEventArgs> OnPacketSending;

        /// <summary>
        /// 在接收到数据包后立即触发。
        /// 订阅此事件可以监视或拦截传入的数据包。
        /// </summary>
        public static event EventHandler<PacketReceivedEventArgs> OnPacketReceived;

        internal static void RaisePacketSending(PacketSendingEventArgs args)
        {
            OnPacketSending?.Invoke(null, args);
        }

        internal static void RaisePacketReceived(PacketReceivedEventArgs args)
        {
            OnPacketReceived?.Invoke(null, args);
        }
        #endregion

        private readonly BossSocketClientBase _client;
        private readonly RemoteAddress _remoteAddress;
        internal static readonly object _directSendFlag = new();

        #region 服务端实现
        // 此构造函数用于创建服务端的监听器实例
        public BossSocket()
        {
            IsServer = true;
        }

        public bool IsServer { get; }

        private static BossSocketServer _server;

        public bool StartListening(SocketConnectionAccepted callback)
        {
            if (_server == null)
            {
                IPAddress address = IPAddress.Any;
                if (Program.LaunchParameters.TryGetValue("-ip", out var value) && !IPAddress.TryParse(value, out address))
                {
                    address = IPAddress.Any;
                }
                _server = new BossSocketServer(address, Netplay.ListenPort, callback);
            }
            return _server.Start();
        }

        public void StopListening()
        {
            _server?.Dispose();
            _server = null;
        }

        // 对于监听器套接字无意义
        public void SendQueuedPackets() { }
        #endregion

        #region 客户端实现
        // 此构造函数用于为每个已接受的连接创建一个客户端套接字实例
        public BossSocket(BossSocketClientBase session)
        {
            _client = session;
            var iPEndPoint = (IPEndPoint)session.Socket.RemoteEndPoint;
            _remoteAddress = new TcpAddress(iPEndPoint.Address, iPEndPoint.Port);
            IsServer = false;
        }

        public void AsyncReceive(byte[] data, int offset, int size, SocketReceiveCallback callback, object state = null)
        {
            // 在此实现中未使用。
            // 接收逻辑由 BossSocketClient.OnReceived 直接处理，数据直接写入 NetMessage.buffer。
        }

        public void AsyncSend(byte[] data, int offset, int size, SocketSendCallback callback, object state = null)
        {
            if (_client == null || !_client.IsConnected)
            {
                callback?.Invoke(state); // 如果未连接，立即完成回调
                return;
            }

            AsyncSendInternal(new ReadOnlyMemory<byte>(data, offset, size), callback, state);
        }
        public void AsyncSend(ReadOnlyMemory<byte> dataToSend, SocketSendCallback callback, object state = null)
        {
            if (_client == null || !_client.IsConnected)
            {
                callback?.Invoke(state); // 如果未连接，立即完成回调
                return;
            }

            AsyncSendInternal(dataToSend, callback, state);
        }
        void AsyncSendInternal(ReadOnlyMemory<byte> dataToSend, SocketSendCallback callback, object state = null)
        {

            // 仅当 RemoteClient 初始化后才触发事件
            if (_client.RemoteClient != null)
            {
                var args = new PacketSendingEventArgs(_client.RemoteClient.Id, dataToSend);
                if (!object.ReferenceEquals(state, BossSocket._directSendFlag))
                {
                    RaisePacketSending(args);

                    if (args.Handled)
                    {
                        // 如果事件处理程序取消了发送，直接调用回调并返回
                        callback?.Invoke(state);
                        return;
                    }
                    // 使用事件处理后可能已修改的数据
                    dataToSend = args.Data;
                }
            }

            // 调用客户端实例的发送方法 (具体是哪种实现取决于实例化的是哪个子类)
            if (_client.SendData(dataToSend))
            {
                // 成功发送或排入队列，我们认为异步发送操作已"完成"，可以调用回调。
                // 这是与Terraria原生Socket行为的适配。
                callback?.Invoke(state);
            }
            else
            {
                // 如果发送/排队失败，记录一个警告。
                BLog.Warn($"[{_client.RemoteClient?.Name ?? _client.Id.ToString()}] 无法发送或排队数据包，可能连接正在关闭。");
                // 同样调用回调，以避免调用方永久等待。
                callback?.Invoke(state);
            }
        }

        public void Close()
        {
            _client?.Dispose();
        }

        public void Connect(RemoteAddress address)
        {
            // 客户端连接逻辑不由该类处理
        }

        public RemoteAddress GetRemoteAddress()
        {
            return _remoteAddress;
        }

        public bool IsConnected()
        {
            // 使用 BossSocketClient 的状态，它直接反映了底层 TCP 连接的状态
            return _client?.IsConnected ?? false;
        }

        public bool IsDataAvailable()
        {
            // 修正: 原实现 (_client?.IsDisposed ?? false) 是错误的。
            // 在我们的架构中，数据被直接推送到 NetMessage.buffer，而不是保留在套接字缓冲区中。
            // Terraria 的主循环需要一个信号来决定是否检查 NetMessage.buffer。
            // 因此，只要客户端处于连接状态，我们就应该返回 true，允许主循环继续进行其消息检查。
            return IsConnected();
        }
        #endregion
    }
}
