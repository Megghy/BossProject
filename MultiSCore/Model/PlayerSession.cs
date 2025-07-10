using System.Buffers;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using BossFramework;
using Microsoft.Xna.Framework;
using MultiSCore.Common;
using MultiSCore.Hooks;
using MultiSCore.Services;
using Terraria;
using Terraria.Localization;
using TShockAPI;

namespace MultiSCore.Model
{
    /// <summary>
    /// 为跨服数据包事件提供数据。
    /// </summary>
    public class PacketEventArgs : System.ComponentModel.CancelEventArgs
    {
        /// <summary>
        /// 触发此事件的玩家会话。
        /// </summary>
        public PlayerSession Session { get; }

        /// <summary>
        /// 数据包的原始数据。可以被事件处理器修改。
        /// </summary>
        public ReadOnlyMemory<byte> PacketData { get; set; }

        /// <summary>
        /// 获取数据包的类型。
        /// </summary>
        public PacketTypes PacketType => PacketData.Length > 2 ? (PacketTypes)PacketData.Span[2] : PacketTypes.ConnectRequest;

        /// <summary>
        /// 初始化 <see cref="PacketEventArgs"/> 的新实例。
        /// </summary>
        /// <param name="session">玩家会话。</param>
        /// <param name="packetData">数据包数据。</param>
        public PacketEventArgs(PlayerSession session, ReadOnlyMemory<byte> packetData)
        {
            Session = session;
            PacketData = packetData;
        }
    }

    /// <summary>
    /// 表示玩家会话的各种状态
    /// </summary>
    public enum SessionState
    {
        /// <summary>
        /// 已断开连接，初始和最终状态。
        /// </summary>
        Disconnected,
        /// <summary>
        /// 正在连接到子服务器。
        /// </summary>
        Connecting,
        /// <summary>
        /// TCP连接成功，正在进行游戏内握手。
        /// </summary>
        Handshaking,
        /// <summary>
        /// 已成功连接到子服务器。
        /// </summary>
        Connected,
        /// <summary>
        /// 正在从子服务器返回主服务器。
        /// </summary>
        Returning
    }

    /// <summary>
    /// 代表一个玩家的跨服会话。
    /// 管理与远程服务器的连接、数据转发和状态备份。
    /// </summary>
    public class PlayerSession(int index, string originalTerrariaVersion, Config config, Version pluginVersion, int timeout = 10) : IDisposable
    {
        /// <summary>
        /// 连接超时时间（秒）
        /// </summary>
        private readonly int CONNECTION_TIMEOUT_SECONDS = timeout;

        public int Index { get; } = index;
        public byte RemoteIndex { get; private set; } = 255;
        public TSPlayer Player => TShock.Players[Index];
        public int SpawnX { get; private set; } = -1;
        public int SpawnY { get; private set; } = -1;

        public ServerInfo TargetServer { get; private set; }
        public SessionState State { get; private set; } = SessionState.Disconnected;

        /// <summary>
        /// 当即将向子服务器发送数据包时触发。
        /// </summary>
        public event EventHandler<PacketEventArgs> PacketSendingToServer;

        /// <summary>
        /// 当从子服务器接收到数据包时触发。
        /// </summary>
        public event EventHandler<PacketEventArgs> PacketReceivedFromServer;
        public event EventHandler<PlayerSession> Disposing;

        private readonly Config _config = config;
        private TcpClient _connection;
        private NetworkStream _stream;
        private CancellationTokenSource _cts;
        private CancellationTokenSource _connectionTimeoutCts;

        private readonly MemoryStream _receiveBuffer = new();
        private PlayerData _playerDataBackup;
        private int _playerDifficultyBackup;
        private readonly string _originalTerrariaVersion = originalTerrariaVersion;
        private readonly Version _pluginVersion = pluginVersion;

        /// <summary>
        /// 切换到目标服务器。
        /// </summary>
        public async Task SwitchServerAsync(ServerInfo serverInfo)
        {
            if (State != SessionState.Disconnected)
            {
                Player.SendErrorMsg("错误：已处于一个跨服会话中或正在切换中。");
                return;
            }

            Player.SetData("MultiSCore.Forwarded", true);
            Player.SetData("MultiSCore.WorldName", serverInfo?.Name);

            TargetServer = serverInfo;
            try
            {
                State = SessionState.Connecting;
                _connectionTimeoutCts = new CancellationTokenSource(TimeSpan.FromSeconds(CONNECTION_TIMEOUT_SECONDS));
                _connection = new TcpClient();

                // 启动超时监控任务
                var timeoutTask = StartConnectionTimeoutMonitoring(_connectionTimeoutCts.Token);

                await _connection.ConnectAsync(TargetServer.IP, TargetServer.Port);
                _stream = _connection.GetStream();

                State = SessionState.Handshaking;
                _cts = new CancellationTokenSource();

                // 备份玩家数据
                BackupPlayerData();

                // 隐藏玩家
                NetMessage.SendData((int)PacketTypes.PlayerActive, -1, Player.Index, null, Index, 0);

                // 发送连接请求
                var builder = new RawDataBuilder(PacketTypes.ConnectRequest)
                    .PackString(TargetServer.Key) // 使用目标服务器的Key
                    .PackString(_config.Name) // 主服名
                    .PackString(Player.IP)
                    .PackString(_pluginVersion.ToString())
                    .PackString(_originalTerrariaVersion);
                await SendToServerAsync(builder.GetByteData());

                // 启动独立的接收任务
                _ = Task.Run(() => ReceiveLoop(_cts.Token), _cts.Token);
            }
            catch (Exception ex)
            {
                HandleConnectionFailure($"连接到 {TargetServer.Name} 失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 启动连接超时监控任务
        /// </summary>
        private async Task StartConnectionTimeoutMonitoring(CancellationToken timeoutToken)
        {
            try
            {
                await Task.Delay(Timeout.Infinite, timeoutToken);
            }
            catch (OperationCanceledException) when (timeoutToken.IsCancellationRequested)
            {
                // 超时检查
                if (State is SessionState.Connecting or SessionState.Handshaking)
                {
                    TShock.Log.ConsoleWarn($"[MultiSCore] 玩家 {Player.Name} 连接到 {TargetServer.Name} 超时 ({CONNECTION_TIMEOUT_SECONDS}秒)");
                    HandleConnectionFailure($"连接到 {TargetServer.Name} 超时");
                }
            }
        }

        /// <summary>
        /// 处理连接失败的统一逻辑
        /// </summary>
        private void HandleConnectionFailure(string errorMessage)
        {
            Dispose();
            Player.SendErrorMsg(errorMessage);
            TShock.Log.ConsoleError($"[MultiSCore] {errorMessage} ({TargetServer.IP}:{TargetServer.Port})");
        }

        /// <summary>
        /// 从子服务器返回主服务器。
        /// </summary>
        public void ReturnToHost()
        {
            if (State != SessionState.Connected) return;

            MSCPlugin.Instance._sessionManager.RemoveSession(Index);

            State = SessionState.Returning;
            int sectionX = Netplay.GetSectionX(0);
            int sectionX2 = Netplay.GetSectionX(Main.maxTilesX);
            int sectionX3 = Netplay.GetSectionX(0);
            int sectionX4 = Netplay.GetSectionX(Main.maxTilesY);
            for (int i = sectionX; i <= sectionX2; i++)
            {
                for (int j = sectionX3; j <= sectionX4; j++)
                {
                    Netplay.Clients[Index].TileSections[i, j] = false;
                }
            }

            // 恢复玩家在本服的可见性和状态
            ForwardToClientDirect(new RawDataBuilder(PacketTypes.ContinueConnecting).PackByte((byte)Index).GetByteData());
            TShock.Players.Where(p => p != null && p.Active && p.Index != Index).ForEach(p =>
            {
                NetMessage.SendData((int)PacketTypes.PlayerActive, Index, -1, null, p.Index, 1);
                NetMessage.SendData((int)PacketTypes.PlayerInfo, Index, -1, null, p.Index);
            });

            Main.npc.ForEach(n => NetMessage.SendData((int)PacketTypes.NpcUpdate, Index, -1, null, n.whoAmI));

            if (TargetServer.RememberHostInventory)
            {
                RestorePlayerData();
            }

            Player.Spawn(PlayerSpawnContext.SpawningIntoWorld);

            if (_config.RememberLastPoint && Player.GetData<Point>("MultiSCore_LastPosition") is Point p)
            {
                Player.Teleport(p.X * 16, p.Y * 16);
            }
            else
            {
                Player.Teleport(Main.spawnTileX * 16, (Main.spawnTileY - 3) * 16);
            }

            NetMessage.SendData((int)PacketTypes.WorldInfo, Index);
            State = SessionState.Disconnected;

            BLog.Info($"[MultiSCore] {Player.Name} 已返回主服.");
        }

        private void BackupPlayerData()
        {
            Player.SaveServerCharacter();
            _playerDataBackup = new PlayerData();
            _playerDifficultyBackup = Player.TPlayer.difficulty;
            Player.SetData("MultiSCore_LastPosition", new Point(Player.TileX, Player.TileY));
        }

        private void RestorePlayerData()
        {
            var sscStatus = Main.ServerSideCharacter;
            Main.ServerSideCharacter = true;
            NetMessage.SendData((int)PacketTypes.WorldInfo, Index);
            Player.TPlayer.difficulty = (byte)_playerDifficultyBackup;
            _playerDataBackup.RestoreCharacter(Player);

            for (int i = 0; i < 59; i++)
            {
                NetMessage.SendData((int)PacketTypes.PlayerSlot, Index, -1, null, Index, i, Player.TPlayer.inventory[i].prefix);
            }
            for (int i = 0; i < Player.TPlayer.armor.Length; i++)
            {
                NetMessage.SendData((int)PacketTypes.PlayerSlot, Index, -1, null, Index, 59 + i, Player.TPlayer.armor[i].prefix);
            }
            for (int i = 0; i < Player.TPlayer.dye.Length; i++)
            {
                NetMessage.SendData((int)PacketTypes.PlayerSlot, Index, -1, null, Index, 59 + Player.TPlayer.armor.Length + i, Player.TPlayer.dye[i].prefix);
            }
            Player.IgnoreSSCPackets = false;
            Main.ServerSideCharacter = sscStatus;
        }

        /// <summary>
        /// 接收来自目标服务器的数据并转发给玩家客户端。
        /// </summary>
        private async Task ReceiveLoop(CancellationToken token)
        {
            var buffer = ArrayPool<byte>.Shared.Rent(102400);
            try
            {
                while (!token.IsCancellationRequested)
                {
                    int bytesRead = await _stream.ReadAsync(buffer, token);
                    if (bytesRead == 0)
                    {
                        // 连接已关闭
                        break;
                    }

                    // 将读取的数据写入缓冲区并处理
                    var originalPosition = _receiveBuffer.Position;
                    _receiveBuffer.Seek(0, SeekOrigin.End);
                    _receiveBuffer.Write(buffer, 0, bytesRead);
                    _receiveBuffer.Position = originalPosition;

                    ProcessReceiveBuffer();
                }
            }
            catch (OperationCanceledException)
            {
                // 任务被取消，正常退出
            }
            catch (Exception ex)
            {
                TShock.Log.ConsoleError($"[MultiSCore] 与 {TargetServer.Name} 的连接中断: {ex.ToString()}");
            }
            finally
            {
                ArrayPool<byte>.Shared.Return(buffer);
                // 非主动返回导致的断线
                if (State is SessionState.Connected or SessionState.Handshaking)
                {
                    TShock.Log.ConsoleInfo($"[MultiSCore] 玩家 {Player.Name} 与 {TargetServer.Name} 的连接意外断开，正在返回主服。");
                    ReturnToHost();
                    Player.SendErrorMsg($"与 {TargetServer.Name} 的连接已断开。");
                }
            }
        }

        public async Task SendToServerAsync(byte[] data, bool isFromClient = false)
        {
            await SendToServerInternalAsync(new Memory<byte>(data), isFromClient);
        }
        public async Task SendToServerAsync(ReadOnlyMemory<byte> data, bool isFromClient = false)
        {
            await SendToServerInternalAsync(data, isFromClient);
        }

        private async Task SendToServerInternalAsync(ReadOnlyMemory<byte> data, bool isFromClient)
        {
            if (_stream == null || !_stream.CanWrite)
                return;

            var args = new PacketEventArgs(this, data);
            OnPacketSendingToServer(args);

            if (args.Cancel)
            {
                return;
            }

            if (NetworkService.IsDebugMode && args.PacketData.Length > 2)
            {
                NetworkService.LogPacket(isFromClient ? "客户端" : "主服", "子服", Index, args.PacketData.Span[2], TargetServer.Name);
            }
            await _stream.WriteAsync(args.PacketData);
        }

        private void ProcessReceiveBuffer()
        {
            while (true)
            {
                var initialPosition = _receiveBuffer.Position;

                // 缓冲区中剩余的数据不足以读取一个完整的包长度字段
                if (_receiveBuffer.Length - initialPosition < 2)
                {
                    break;
                }

                // 读取包长
                var lengthBuffer = new byte[2];
                _receiveBuffer.Read(lengthBuffer, 0, 2);
                var packetLength = BitConverter.ToUInt16(lengthBuffer, 0);
                _receiveBuffer.Position = initialPosition; // 重置位置以便后续完整读取

                // 检查缓冲区中是否有完整的数据包
                if (_receiveBuffer.Length - initialPosition < packetLength)
                {
                    // 数据包不完整，等待更多数据
                    break;
                }

                // 读取完整的数据包
                var packetData = new byte[packetLength];
                _receiveBuffer.Read(packetData, 0, packetLength);

                ProcessSinglePacket(new Memory<byte>(packetData));

                // 如果在处理数据包后玩家断开连接，则停止处理
                if (State == SessionState.Disconnected)
                {
                    break;
                }
            }

            // 清理已处理的数据，压缩缓冲区
            if (_receiveBuffer.Position == _receiveBuffer.Length)
            {
                _receiveBuffer.SetLength(0);
                _receiveBuffer.Position = 0;
            }
            else if (_receiveBuffer.Position > 0)
            {
                var remainingBytes = _receiveBuffer.ToArray().AsSpan((int)_receiveBuffer.Position).ToArray();
                _receiveBuffer.SetLength(0);
                _receiveBuffer.Write(remainingBytes, 0, remainingBytes.Length);
                _receiveBuffer.Position = 0;
            }
        }

        private void ProcessSinglePacket(Memory<byte> currentPacket)
        {
            var args = new PacketEventArgs(this, currentPacket);
            OnPacketReceivedFromServer(args);

            if (args.Cancel)
            {
                return;
            }
            if (args.PacketData.Span != currentPacket.Span)
            {
                currentPacket = args.PacketData.ToArray();
            }

            var packetIdByte = currentPacket.Span[2];

            var packetId = (PacketTypes)packetIdByte;
            switch (packetId)
            {
                case PacketTypes.Disconnect: // 2
                    {
                        using var reader = new BinaryReader(new MemoryStream(currentPacket[3..].ToArray()));
                        var reason = NetworkText.Deserialize(reader).ToString();
                        TShock.Log.Info($"{Player.Name} 被子服务器 '{TargetServer.Name}' 断开连接，原因: {reason}");
                        Player.SendErrorMsg($"已从 {TargetServer.Name} 断开连接: {reason}");

                        // 取消超时监控
                        _connectionTimeoutCts?.Cancel();

                        ReturnToHost();
                        return;
                    }

                case PacketTypes.ContinueConnecting: // 3
                    if (currentPacket.Length >= 4)
                    {
                        RemoteIndex = currentPacket.Span[3];
                        //Console.WriteLine($"[MultiSCore] 玩家 {Player.Name} 在子服务器 {TargetServer.Name} 的远程索引为 {RemoteIndex}。");
                    }
                    break;

                case PacketTypes.WorldInfo: // 7
                    {

                        if (State == SessionState.Handshaking)
                        {
                            using var s = new MemoryStream(currentPacket.ToArray());
                            using var reader = new BinaryReader(s);
                            // 提取出生点坐标
                            var worldInfo = BossFramework.BNet.PacketHandler.Serializer.Deserialize(reader) as TrProtocol.Packets.WorldData;
                            if (SpawnX == -1)
                            {
                                //Console.WriteLine($"[MultiSCore] {Player.Name} 已设定世界出生点, SpawnX: {SpawnX}, SpawnY: {SpawnY}");
                            }
                            SpawnX = worldInfo.SpawnX;
                            SpawnY = worldInfo.SpawnY;
                            // 发送出生点数据包
                            var spawnDataPacket = new RawDataBuilder(PacketTypes.TileGetSection)
                                .PackInt32(SpawnX)
                                .PackInt32(SpawnY);
                            _ = SendToServerAsync(new TrProtocol.Packets.RequestTileData()
                            {
                                Position = new((short)SpawnX, (short)SpawnY),
                            }.SerializePacket());
                        }

                        break;
                    }
                case PacketTypes.PlayerSpawnSelf:
                    // 发送玩家出生包
                    _ = SendToServerAsync(new TrProtocol.Packets.SpawnPlayer()
                    {
                        Context = TrProtocol.Models.PlayerSpawnContext.SpawningIntoWorld,
                        DeathsPVE = (short)Player.TPlayer.numberOfDeathsPVE,
                        DeathsPVP = (short)Player.TPlayer.numberOfDeathsPVP,
                        PlayerSlot = RemoteIndex,
                        Position = new((short)SpawnX, (short)SpawnY),
                        Timer = 0
                    }.SerializePacket());
                    break;
                case PacketTypes.FinishedConnectingToServer:
                    if (State != SessionState.Connected)
                    {
                        State = SessionState.Connected;

                        // 连接成功，取消超时监控
                        _connectionTimeoutCts?.Cancel();
                        _connectionTimeoutCts?.Dispose();
                        _connectionTimeoutCts = null;

                        MSCHooks.InvokePlayerFinishSwitch(Player, this);
                        if (SpawnX == -1 || SpawnY == -1)
                        {

                        }
                        else
                        {
                            ForwardToClientDirect(new TrProtocol.Packets.Teleport()
                            {
                                Position = new(SpawnX * 16, (SpawnY - 3) * 16),
                                PlayerSlot = RemoteIndex,
                                Bit1 = 0,
                                Style = 1,
                            }.SerializePacket());
                        }
                        TShock.Log.ConsoleInfo($"[MultiSCore] {Player.Name} 已传送至子服务器 {TargetServer.Name}");
                    }
                    break;
                case (PacketTypes)15: // Custom packet from old implementation (NetModuleLoad)
                                      //TShock.Log.ConsoleInfo($"[MSC] 收到来自子服务器的自定义数据包，已按设计阻止转发。");
                    return;

                case PacketTypes.PasswordRequired: // 37
                    Player.SendErrorMsg($"服务器 '{TargetServer.Name}' 需要密码。");

                    // 取消超时监控
                    _connectionTimeoutCts?.Cancel();

                    ReturnToHost();
                    return;
            }

            ForwardToClientDirect(currentPacket);
        }

        private void ForwardToClient(Memory<byte> data)
        {
            if (MemoryMarshal.TryGetArray((ReadOnlyMemory<byte>)data, out var segment))
            {
                // 高效方式：直接使用底层数组的段，避免分配新数组
                SendArraySegment(segment);
            }
            else
            {
                // 备用方式：如果内存不是由数组支持，则创建一个新数组
                Player.SendRawData(data.ToArray());
            }
        }
        /// <summary>
        /// 直接发送到玩家客户端, 不会触发Hook
        /// </summary>
        /// <param name="data"></param>
        private void ForwardToClientDirect(Memory<byte> data)
        {
            if (NetworkService.IsDebugMode)
            {
                NetworkService.LogPacket("子服", "客户端", Index, data.Span[2], TargetServer.Name);
            }
            BossFramework.BUtils.SendRawDataDirect(Player, data);
        }

        /// <summary>
        /// 发送字节数组的一个片段。这是一个辅助方法，用于处理 SendRawData 不接受 ArraySegment 的情况。
        /// </summary>
        private void SendArraySegment(ArraySegment<byte> segment)
        {
            // 如果片段正好是整个数组，直接发送，避免分配。
            if (segment.Offset == 0 && segment.Count == segment.Array.Length)
            {
                BossFramework.BUtils.SendRawDataDirect(Player, segment.Array);
                return;
            }

            // 创建一个精确大小的新数组并发送。
            var newData = new byte[segment.Count];
            Buffer.BlockCopy(segment.Array, segment.Offset, newData, 0, segment.Count);
            BossFramework.BUtils.SendRawDataDirect(Player, newData);
        }

        private void DisposeConnection()
        {
            _cts?.Cancel();
            _cts?.Dispose();
            _connectionTimeoutCts?.Cancel();
            _connectionTimeoutCts?.Dispose();
            _stream?.Close();
            _stream?.Dispose();
            _connection?.Close();
            _cts = null;
            _connectionTimeoutCts = null;
            _stream = null;
            _connection = null;
        }

        /// <summary>
        /// 触发 <see cref="PacketSendingToServer"/> 事件。
        /// </summary>
        /// <param name="e">包含事件数据的 <see cref="PacketEventArgs"/>。</param>
        protected virtual void OnPacketSendingToServer(PacketEventArgs e)
        {
            PacketSendingToServer?.Invoke(this, e);
        }

        /// <summary>
        /// 触发 <see cref="PacketReceivedFromServer"/> 事件。
        /// </summary>
        /// <param name="e">包含事件数据的 <see cref="PacketEventArgs"/>。</param>
        protected virtual void OnPacketReceivedFromServer(PacketEventArgs e)
        {
            PacketReceivedFromServer?.Invoke(this, e);
        }
        bool disposed = false;
        public void Dispose()
        {
            if (disposed)
            {
                return;
            }
            disposed = true;

            Player.RemoveData("MultiSCore.Forwarded");
            Player.RemoveData("MultiSCore.WorldName");

            //TShock.Log.ConsoleInfo($"[MultiSCore] {Player.Name} 会话已释放.");
            Disposing?.Invoke(this, this);
            DisposeConnection();
            MSCPlugin.Instance._sessionManager.RemoveSession(Index);
            State = SessionState.Disconnected;
        }
    }
}