using System.Buffers;
using System.IO.Pipelines;
using System.Net.Sockets;
using System.Threading.Channels;
using NetCoreServer;
using Terraria;
using Terraria.Net.Sockets;

namespace BossFramework.BNet
{
    #region Base Class
    /// <summary>
    /// TCP 客户端会话的抽象基类.
    /// 包含接收逻辑和通用生命周期管理. 发送逻辑由派生类实现.
    /// </summary>
    public abstract class BossSocketClientBase : TcpSession
    {
        public RemoteClient RemoteClient { get; internal set; }

        protected readonly SocketConnectionAccepted _callback;
        protected readonly CancellationTokenSource _cancellationSource = new();
        private readonly Pipe _receivePipe = new();
        private Task _receiveTask;
        private string _remoteAddress;

        protected BossSocketClientBase(TcpServer server, SocketConnectionAccepted callback) : base(server)
        {
            _callback = callback;
        }

        /// <summary>
        /// 派生类必须实现此方法以定义其发送数据的策略.
        /// </summary>
        public abstract bool SendData(ReadOnlyMemory<byte> data);

        /// <summary>
        /// 派生类必须实现此方法以初始化其特定的后台任务 (例如发送循环).
        /// </summary>
        protected abstract void InitializeTasks();

        protected override void OnConnecting()
        {
            _remoteAddress = Socket.RemoteEndPoint?.ToString();
            var socket = new BossSocket(this);
            try
            {
                //BLog.Log($"[{_remoteAddress ?? "未知地址"}] 正在连接...");

                _callback(socket);
                RemoteClient = Netplay.Clients.FirstOrDefault(rc => rc?.Socket == socket);

                if (RemoteClient is null)
                {
                    BLog.Warn($"[{socket.GetRemoteAddress()}] 未找到匹配的 RemoteClient, 连接关闭.");
                    socket.Close();
                    return;
                }

                InitializeTasks(); // 初始化派生类任务
                _receiveTask = Task.Run(ReceiveLoopAsync, CancellationToken.None); // 接收任务是通用的

                BLog.Info($"客户端 [{RemoteClient.Name}@{_remoteAddress}] 连接成功. ID: {RemoteClient.Id} 使用 {GetType().Name}.");
            }
            catch (Exception ex)
            {
                BLog.Error($"[{_remoteAddress ?? "未知地址"}] 处理连接时发生异常: {ex.Message}");
                Disconnect();
            }
        }

        protected override void OnDisconnected()
        {
            if (!_cancellationSource.IsCancellationRequested)
                _cancellationSource.Cancel();

            _receivePipe.Writer.Complete();

            var clientInfo = RemoteClient?.Name ?? "未知用户";
            var address = _remoteAddress ?? "未知地址";
            BLog.Log($"[{clientInfo}@{address}] 已断开连接.");
        }

        protected override void OnReceived(byte[] buffer, long offset, long size)
        {
            if (RemoteClient is null || size <= 0 || _cancellationSource.IsCancellationRequested) return;

            try
            {
                _receivePipe.Writer.Write(buffer.AsSpan((int)offset, (int)size));
                var flushTask = _receivePipe.Writer.FlushAsync();

                if (flushTask.IsFaulted)
                    BLog.Warn($"[{RemoteClient.Name}] 写入接收管道时同步失败.");
            }
            catch (Exception ex)
            {
                BLog.Error($"[{RemoteClient.Name}] 写入接收管道时出错: {ex.Message}");
                _cancellationSource.Cancel();
            }
        }

        private async Task ReceiveLoopAsync()
        {
            var reader = _receivePipe.Reader;
            try
            {
                while (!_cancellationSource.IsCancellationRequested)
                {
                    var result = await reader.ReadAsync(_cancellationSource.Token);
                    var buffer = result.Buffer;

                    if (buffer.IsEmpty && result.IsCompleted) break;

                    var consumed = ProcessPackets(buffer);
                    reader.AdvanceTo(consumed, buffer.End);

                    if (result.IsCompleted) break;
                }
            }
            catch (OperationCanceledException) { /* 正常取消 */ }
            catch (Exception ex)
            {
                BLog.Error($"[{Id}] 接收循环异常: {ex.Message}");
                _cancellationSource.Cancel();
            }
            finally
            {
                await reader.CompleteAsync();
            }
        }

        private SequencePosition ProcessPackets(ReadOnlySequence<byte> buffer)
        {
            var reader = new SequenceReader<byte>(buffer);

            while (reader.Remaining >= 2)
            {
                var tempReader = reader;
                if (!tempReader.TryReadLittleEndian(out short packetLengthValue)) break;

                var packetLength = (ushort)packetLengthValue;

                if (packetLength < 3)
                {
                    BLog.Warn($"[{Id}] 接收到无效的数据包长度: {packetLength}, 连接将断开.");
                    _cancellationSource.Cancel();
                    return buffer.End;
                }

                if (reader.Remaining < packetLength) break;

                var packetData = reader.Sequence.Slice(reader.Position, packetLength);
                ProcessPacket(packetData);
                reader.Advance(packetLength);
            }
            return reader.Position;
        }

        private void ProcessPacket(ReadOnlySequence<byte> packetData)
        {
            if (RemoteClient is null) return;

            byte[] rentedBuffer = null;
            try
            {
                ReadOnlyMemory<byte> packetMemory;
                if (packetData.IsSingleSegment)
                {
                    packetMemory = packetData.First;
                }
                else
                {
                    var length = (int)packetData.Length;
                    rentedBuffer = ArrayPool<byte>.Shared.Rent(length);
                    packetData.CopyTo(rentedBuffer.AsSpan(0, length));
                    packetMemory = new ReadOnlyMemory<byte>(rentedBuffer, 0, length);
                }

                var eventArgs = new PacketReceivedEventArgs(RemoteClient.Id, packetMemory);
                BossSocket.RaisePacketReceived(eventArgs);

                if (!eventArgs.Handled)
                    WriteToTerrariaBuffer(eventArgs.Data.Span);
            }
            finally
            {
                if (rentedBuffer is not null)
                    ArrayPool<byte>.Shared.Return(rentedBuffer);
            }
        }

        private void WriteToTerrariaBuffer(ReadOnlySpan<byte> data)
        {
            if (RemoteClient is null) return;
            var messageBuffer = NetMessage.buffer[RemoteClient.Id];
            lock (messageBuffer)
            {
                var currentTotalData = messageBuffer.totalData;
                var availableSpace = messageBuffer.readBuffer.Length - currentTotalData;
                if (data.Length <= availableSpace)
                {
                    data.CopyTo(messageBuffer.readBuffer.AsSpan(currentTotalData));
                    messageBuffer.totalData = currentTotalData + data.Length;
                    messageBuffer.checkBytes = true;
                }
                else
                {
                    BLog.Warn($"[{RemoteClient.Name}] Terraria缓冲区不足, 丢弃包. 需要: {data.Length}, 可用: {availableSpace}");
                }
            }
        }

        protected override void OnError(SocketError error)
        {
            BLog.Warn($"[{RemoteClient?.Name ?? Id.ToString()}] 套接字错误: {error}");
            _cancellationSource.Cancel();
        }

        public bool IsHealthy() => IsConnected && !_cancellationSource.IsCancellationRequested;

        protected override void Dispose(bool disposing)
        {
            if (IsDisposed) return;
            if (disposing)
            {
                if (!_cancellationSource.IsCancellationRequested)
                    _cancellationSource.Cancel();

                try
                {
                    // 等待接收任务
                    if (_receiveTask != null && !_receiveTask.Wait(TimeSpan.FromSeconds(2)))
                        BLog.Warn($"[{Id}] 接收任务关闭超时.");
                }
                catch (AggregateException ex)
                {
                    if (ex.InnerExceptions.Any(e => e is not OperationCanceledException))
                        BLog.Warn($"[{Id}] Dispose期间等待接收任务时发生非预期的异常: {ex.GetBaseException()}");
                }

                _cancellationSource.Dispose();
            }
            base.Dispose(disposing);
        }
    }
    #endregion

    #region Direct Send Client
    /// <summary>
    /// 低延迟客户端, 使用锁直接发送数据.
    /// </summary>
    public sealed class DirectSendSocketClient : BossSocketClientBase
    {
        private readonly object _sendLock = new();

        public DirectSendSocketClient(TcpServer server, SocketConnectionAccepted callback) : base(server, callback)
        {
            server.OptionNoDelay = true;
        }

        protected override void InitializeTasks() { /* 无需额外后台任务 */ }

        public override bool SendData(ReadOnlyMemory<byte> data)
        {
            if (!IsConnected || data.IsEmpty) return true;

            lock (_sendLock)
            {
                if (!IsConnected) return false;
                if (SendAsync(data.Span))
                {
                    return true;
                }
                else
                {
                    BLog.Warn($"[{Id}] 直接发送失败, 断开连接");
                    _cancellationSource.Cancel();
                    return false;
                }
            }
        }
    }
    #endregion

    #region Channel Send Client
    /// <summary>
    /// 高吞吐量客户端, 使用Channel将发送操作排入后台队列.
    /// </summary>
    public sealed class ChannelSendSocketClient : BossSocketClientBase
    {
        private readonly struct SendPayload
        {
            public byte[] RentedBuffer { get; }
            public int Length { get; }
            public SendPayload(byte[] rentedBuffer, int length)
            {
                RentedBuffer = rentedBuffer;
                Length = length;
            }
        }

        private Channel<SendPayload> _sendChannel;
        private Task _sendTask;

        public ChannelSendSocketClient(TcpServer server, SocketConnectionAccepted callback) : base(server, callback) { }

        protected override void InitializeTasks()
        {
            _sendChannel = Channel.CreateUnbounded<SendPayload>(new UnboundedChannelOptions
            {
                SingleReader = true,
                SingleWriter = false
            });
            _sendTask = Task.Run(SendLoopAsync);
        }

        public override bool SendData(ReadOnlyMemory<byte> data)
        {
            if (!IsHealthy())
                return false;
            if (data.IsEmpty)
                return true;

            var buffer = ArrayPool<byte>.Shared.Rent(data.Length);
            data.CopyTo(buffer);

            if (_sendChannel.Writer.TryWrite(new SendPayload(buffer, data.Length)))
            {
                return true;
            }

            ArrayPool<byte>.Shared.Return(buffer);
            BLog.Warn($"[{RemoteClient?.Name}] 发送队列写入失败. 通道可能已关闭.");
            return false;
        }

        private async Task SendLoopAsync()
        {
            try
            {
                await foreach (var payload in _sendChannel.Reader.ReadAllAsync(_cancellationSource.Token))
                {
                    try
                    {
                        if (!SendAsync(new ReadOnlySpan<byte>(payload.RentedBuffer, 0, payload.Length)))
                        {
                            BLog.Warn($"[{Id}] 队列发送失败, 断开连接");
                            _cancellationSource.Cancel();
                            break;
                        }
                    }
                    finally
                    {
                        ArrayPool<byte>.Shared.Return(payload.RentedBuffer);
                    }
                }
            }
            catch (OperationCanceledException)
            {
                // 正常取消
            }
            finally
            {
                // 确保在循环退出后(无论是正常完成、异常还是break), 都能排空队列并释放所有剩余的缓冲区
                while (_sendChannel.Reader.TryRead(out var payload))
                {
                    ArrayPool<byte>.Shared.Return(payload.RentedBuffer);
                }
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (IsDisposed) return;
            if (disposing)
            {
                _sendChannel?.Writer.TryComplete();
                try
                {
                    if (_sendTask != null && !_sendTask.Wait(TimeSpan.FromSeconds(2)))
                        BLog.Warn($"[{Id}] 发送任务关闭超时.");
                }
                catch (AggregateException ex)
                {
                    if (ex.InnerExceptions.Any(e => e is not OperationCanceledException))
                        BLog.Warn($"[{Id}] Dispose期间等待发送任务时发生非预期的异常: {ex.GetBaseException()}");
                }
            }
            base.Dispose(disposing);
        }
    }
    #endregion
}

