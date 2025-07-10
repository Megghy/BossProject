using System.Net;
using System.Net.Sockets;
using NetCoreServer;
using Terraria.Net.Sockets;
using TShockAPI;

namespace BossFramework.BNet
{
    /// <summary>
    /// BossSocket TCP 服务器实现.
    /// 基于 NetCoreServer 库.
    /// </summary>
    public sealed class BossSocketServer(IPAddress address, int port, SocketConnectionAccepted callback) : TcpServer(address, port)
    {
        private readonly SocketConnectionAccepted _callback = callback;

        /// <summary>
        /// 当有新连接时，为此连接创建一个会话实例
        /// </summary>
        protected override TcpSession CreateSession()
        {
            var threshold = BConfig.Instance.NetworkSwitchThreshold;
            var activePlayers = TShock.Players.Count(p => p?.Active ?? false);

            if (threshold != -1 && activePlayers >= threshold)
            {
                // 人数达到阈值，使用高吞吐量模式
                return new ChannelSendSocketClient(this, _callback);
            }
            else
            {
                // 默认或人数未达阈值，使用低延迟模式
                return new DirectSendSocketClient(this, _callback);
            }
        }

        /// <summary>
        /// 捕获并记录底层套接字错误
        /// </summary>
        protected override void OnError(SocketError error)
        {
            // 使用 BLog 记录错误，以保持项目日志系统的一致性
            BLog.Error($"服务器套接字捕获到错误: {error}");
        }
    }
}
