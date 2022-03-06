using NetCoreServer;
using System;
using System.Net;
using System.Net.Sockets;
using Terraria.Net.Sockets;

namespace BossFramework.BNet
{
    public sealed class BossSocketServer : TcpServer
    {
        private SocketConnectionAccepted _callback;

        public BossSocketServer(IPAddress address, int port, SocketConnectionAccepted callback) : base(address, port) { _callback = callback; }
        protected override TcpSession CreateSession()
            => new BossSocketClient(this, _callback);

        protected override void OnError(SocketError error)
        {
            Console.WriteLine($"网络异常\r\n{error}");
        }
    }
}
