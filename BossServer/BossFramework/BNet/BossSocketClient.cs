using NetCoreServer;
using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Net.Sockets;
using System.Threading;
using Terraria;
using Terraria.Net.Sockets;

namespace BossFramework.BNet
{
    public sealed class BossSocketClient : TcpSession
    {
        public RemoteClient RemoteClient { get; private set; }
        private readonly SocketConnectionAccepted _callback;
        private bool _shouldStop = false;

        internal readonly BlockingCollection<Action> _sendQueue = new();

        public BossSocketClient(BossSocketServer server, SocketConnectionAccepted callback) : base(server)
        {
            _callback = callback;
        }
        public void SendLoop()
        {
            while (!_shouldStop && !Netplay.Disconnect)
            {
                try
                {
                    _sendQueue.Take()();
                }
                catch (Exception ex) { BLog.Error(ex); }
            }
        }
        protected override void OnConnecting()
        {
            try
            {
                var socket = new BossSocket(this);
                BLog.Log($"{socket.GetRemoteAddress()} 正在连接...");
                _callback?.Invoke(socket);
                RemoteClient = Netplay.Clients.SingleOrDefault((RemoteClient x) => x != null && x.Socket == socket);
                if (RemoteClient is null)
                    socket.Close();
                else
                    new Thread(SendLoop)
                    {
                        IsBackground = true,
                        Name = $"Boss Socket Send Loop:{Socket.RemoteEndPoint}"
                    }.Start();
            }
            catch (Exception value2)
            {
                Console.WriteLine(value2);
            }
        }
        protected override void OnDisconnected()
        {
            BLog.Log($"{RemoteClient?.Name ?? RemoteClient.Socket.GetRemoteAddress().ToString()} 断开连接");
            Dispose();
        }

        protected override void OnReceived(byte[] buffer, long offset, long size)
        {
            if (!RemoteClient.IsActive)
            {
                RemoteClient.IsActive = true;
                RemoteClient.State = 0;
            }
            var target = NetMessage.buffer[RemoteClient.Id];
            lock (target)
            {
                System.Buffer.BlockCopy(buffer, (int)offset, target.readBuffer, target.totalData, (int)size);
                target.totalData += (int)size;
                target.checkBytes = true;
            }
        }
        protected override void OnError(SocketError error)
        {
            BLog.Warn($"TCP session caught an error with code {error}");
        }
        protected override void Dispose(bool disposingManagedResources)
        {
            _sendQueue.Dispose();
            _shouldStop = true;
            base.Dispose(disposingManagedResources);
        }
    }
}
