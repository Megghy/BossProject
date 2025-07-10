#region Using
using Terraria;
using Terraria.Net.Sockets;
using TerrariaApi.Server;
#endregion

namespace FakeProvider.Handlers
{
    /// <summary>
    /// 处理假Provider的网络数据发送相关逻辑
    /// </summary>
    internal static class NetworkHandler
    {
        private static readonly object _providerLock = new object();

        #region 初始化和清理

        /// <summary>
        /// 注册网络处理相关的事件
        /// </summary>
        public static void Initialize()
        {
            return;
            ServerApi.Hooks.NetSendData.Register(FakeProviderPlugin.Instance, OnSendData, Int32.MaxValue);
            ServerApi.Hooks.ServerLeave.Register(FakeProviderPlugin.Instance, OnServerLeave);
        }

        /// <summary>
        /// 取消注册网络处理相关的事件
        /// </summary>
        public static void Dispose()
        {
            ServerApi.Hooks.NetSendData.Deregister(FakeProviderPlugin.Instance, OnSendData);
            ServerApi.Hooks.ServerLeave.Deregister(FakeProviderPlugin.Instance, OnServerLeave);
        }

        #endregion

        #region 数据发送处理

        /// <summary>
        /// 处理网络数据发送事件，拦截并自定义处理地图相关的数据包
        /// </summary>
        private static void OnSendData(SendDataEventArgs args)
        {
            if (args.Handled)
                return;

            switch (args.MsgId)
            {
                case PacketTypes.TileSendSection:
                    if (!FakeProviderPlugin.ProvidersLoaded) return;
                    args.Handled = true;
                    // We allow sending packet to custom list of players by specifying it in text parameter
                    if (args.text?._text?.Length > 0)
                        SendSectionPacket.Send(args.text._text.Select(c => (int)c), args.ignoreClient,
                            args.number, (int)args.number2, (short)args.number3, (short)args.number4);
                    else
                        SendSectionPacket.Send(args.remoteClient, args.ignoreClient,
                            args.number, (int)args.number2, (short)args.number3, (short)args.number4);
                    break;
                case PacketTypes.TileFrameSection:
                    if (!FakeProviderPlugin.ProvidersLoaded) return;
                    args.Handled = true;
                    // We allow sending packet to custom list of players by specifying it in text parameter
                    if (args.text?._text?.Length > 0)
                        FrameSectionPacket.Send(args.text._text.Select(c => (int)c), args.ignoreClient,
                            (short)args.number, (short)args.number2, (short)args.number3, (short)args.number4);
                    else
                        FrameSectionPacket.Send(args.remoteClient, args.ignoreClient,
                            (short)args.number, (short)args.number2, (short)args.number3, (short)args.number4);
                    break;
                case PacketTypes.TileSendSquare:
                    if (!FakeProviderPlugin.ProvidersLoaded) return;
                    args.Handled = true;
                    // We allow sending packet to custom list of players by specifying it in text parameter
                    if (args.text?._text?.Length > 0)
                        SendTileSquarePacket.Send(args.text._text.Select(c => (int)c), args.ignoreClient,
                            (int)args.number3, (int)args.number4, (int)args.number, (int)args.number2, args.number5);
                    else
                        SendTileSquarePacket.Send(args.remoteClient, args.ignoreClient,
                            (int)args.number3, (int)args.number4, (int)args.number, (int)args.number2, args.number5);
                    break;
            }
        }

        /// <summary>
        /// 处理玩家离开服务器事件，清理该玩家相关的个人Provider
        /// </summary>
        private static void OnServerLeave(LeaveEventArgs args)
        {
            var playerIndex = args.Who;
            if (playerIndex < 0 || playerIndex >= Main.maxPlayers)
                return;

            lock (_providerLock)
            {
                // ToList() 创建一个快照以在枚举期间安全地修改集合
                var personalProviders = FakeProviderAPI.Tile.Personal.ToList();

                foreach (var provider in personalProviders)
                {
                    // 假设 Observers 是一个可修改的集合, 如 List 或 HashSet
                    // 更健壮的方法是在 Provider 上提供一个专用方法，例如 provider.RemoveObserver(playerIndex)
                    if (provider.Observers.Contains(playerIndex))
                    {
                        if (provider.Observers is ICollection<int> observers) // 检查它是否是可修改的集合
                        {
                            observers.Remove(playerIndex);
                        }

                        // 如果我们移除了最后一个观察者，这个提供者就成了孤儿，可以被移除
                        if (!provider.Observers.Any())
                        {
                            FakeProviderAPI.Tile.Remove(provider);
                        }
                    }
                }
            }
        }

        #endregion

        #region 网络发送工具

        /// <summary>
        /// 向指定的客户端发送数据
        /// </summary>
        /// <param name="clients">目标客户端列表</param>
        /// <param name="data">要发送的数据</param>
        internal static void SendTo(IEnumerable<RemoteClient> clients, byte[] data)
        {
            foreach (RemoteClient client in clients)
                try
                {
                    client.Socket.AsyncSend(data, 0, data.Length,
                        new SocketSendCallback(client.ServerWriteCallBack), null);
                }
                catch (IOException) { }
                catch (ObjectDisposedException) { }
                catch (InvalidOperationException) { }
        }

        #endregion
    }
}