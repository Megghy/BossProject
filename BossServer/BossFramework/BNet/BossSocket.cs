using System.Net;
using Terraria;
using Terraria.Net;
using Terraria.Net.Sockets;

namespace BossFramework.BNet
{
    internal class BossSocket : ISocket
    {
        private readonly BossSocketClient _client;
        private readonly RemoteAddress _remoteAddress;

        /// <summary>
        /// 服务端处理
        /// </summary>
        public BossSocket()
        {
            IsServer = true;
        }

        public bool IsServer { get; private set; }

        #region 处理客户端的东西
        /// <summary>
        /// 创建客户端处理
        /// </summary>
        /// <param name="session"></param>
        public BossSocket(BossSocketClient session)
        {
            _client = session;
            IPEndPoint iPEndPoint = (IPEndPoint)session.Socket.RemoteEndPoint;
            _remoteAddress = new TcpAddress(iPEndPoint.Address, iPEndPoint.Port);
            IsServer = false;

        }

        public void AsyncReceive(byte[] data, int offset, int size, SocketReceiveCallback callback, object state = null)
        {
            //没用
        }

        public void AsyncSend(byte[] data, int offset, int size, SocketSendCallback callback, object state = null)
        {
            _client?._sendQueue.Enqueue(delegate
            {
                _client?.Send(data, offset, size);
                callback?.Invoke(state);
            });
        }

        public void Close()
        {
            _client?.Dispose();
        }

        public void Connect(RemoteAddress address)
        {
            //这是客户端里的
        }

        public RemoteAddress GetRemoteAddress()
        {
            return _remoteAddress;
        }

        public bool IsConnected()
        {
            return _client?.IsConnected ?? false;
        }

        public bool IsDataAvailable()
        {
            return _client?.IsDisposed ?? false;
        }

        public void SendQueuedPackets()
        {
            //没用
        }
        #endregion

        #region 服务端的
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
            _server.Dispose();
            _server = null;
        }
        #endregion
    }
}
