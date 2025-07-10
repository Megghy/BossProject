using System.Threading.Channels;
using BossFramework.BAttributes;
using BossFramework.BInterfaces;
using BossFramework.BModels;

using Terraria;
using TrProtocol.Packets;

namespace BossFramework.BNet.PacketHandlers
{
    public class SyncProjectileHandler : PacketHandlerBase<SyncProjectile>
    {
        private static readonly CancellationTokenSource _cancellationTokenSource = new();
        private static readonly SemaphoreSlim _processingSemaphore = new(1, 1);

        // 使用Channel替代ConcurrentQueue，提供更好的背压控制
        private static readonly Channel<SyncProjectile> _projChannel =
            Channel.CreateUnbounded<SyncProjectile>();

        // 批处理配置
        private const int BATCH_SIZE = 50;
        private const int MAX_WAIT_MS = 5;

        [AutoInit]
        public static void InitProjQueue()
        {
            // 使用配置的线程数，默认为CPU核心数的一半
            int workerCount = Math.Max(1, Environment.ProcessorCount / 2);

            for (int i = 0; i < workerCount; i++)
            {
                _ = Task.Run(ProcessProjLoopAsync, _cancellationTokenSource.Token);
            }
        }

        private static async Task ProcessProjLoopAsync()
        {
            var batch = new List<SyncProjectile>(BATCH_SIZE);

            try
            {
                await foreach (var proj in _projChannel.Reader.ReadAllAsync(_cancellationTokenSource.Token))
                {
                    batch.Add(proj);

                    // 当批次满了或者等待时间超过阈值时处理批次
                    if (batch.Count >= BATCH_SIZE ||
                        (batch.Count > 0 && await WaitForNextOrTimeout(batch.Count)))
                    {
                        await ProcessBatch(batch);
                        batch.Clear();
                    }
                }
            }
            catch (OperationCanceledException) when (_cancellationTokenSource.Token.IsCancellationRequested)
            {
                // 优雅关闭，处理剩余项目
                if (batch.Count > 0)
                {
                    await ProcessBatch(batch);
                }
            }
            catch (Exception ex)
            {
                BLog.Error($"弹幕处理循环异常: {ex}");
            }
        }

        private static async Task<bool> WaitForNextOrTimeout(int currentBatchSize)
        {
            using var timeoutCts = new CancellationTokenSource(MAX_WAIT_MS);
            using var combinedCts = CancellationTokenSource.CreateLinkedTokenSource(
                _cancellationTokenSource.Token, timeoutCts.Token);

            try
            {
                await _projChannel.Reader.WaitToReadAsync(combinedCts.Token);
                return false; // 有新数据，继续收集
            }
            catch (OperationCanceledException)
            {
                return timeoutCts.Token.IsCancellationRequested; // 超时，处理当前批次
            }
        }

        private static async Task ProcessBatch(List<SyncProjectile> batch)
        {
            // 使用信号量确保不会超载主线程
            await _processingSemaphore.WaitAsync(_cancellationTokenSource.Token);

            try
            {
                // 在主线程上批量处理
                await Task.Run(() =>
                {
                    foreach (var proj in batch)
                    {
                        ProcessSingleProjectile(proj);
                    }
                }, _cancellationTokenSource.Token);
            }
            finally
            {
                _processingSemaphore.Release();
            }
        }

        private static void ProcessSingleProjectile(SyncProjectile proj)
        {
            try
            {
                // 数据验证和预处理
                if (proj.UUID >= 1000)
                    proj.UUID = -1;

                if (proj.ProjType == 949)
                    proj.PlayerSlot = 255;
                else if (Main.projHostile[proj.ProjType])
                    return;

                // 优化：使用Span<T>减少数组访问开销
                var projectiles = Main.projectile.AsSpan();
                int projSlot = FindProjectileSlot(projectiles, proj);

                if (projSlot >= 1000)
                    return; // 无法找到合适的槽位

                ref var projectile = ref projectiles[projSlot];

                // 批量设置属性，减少多次访问开销
                SetProjectileProperties(ref projectile, proj);

                // 异步发送网络消息，避免阻塞
                _ = Task.Run(() => NetMessage.TrySendData(27, -1, proj.PlayerSlot, null, projSlot));
            }
            catch (Exception ex)
            {
                BLog.Error($"处理单个弹幕时发生错误: {ex}");
            }
        }

        private static int FindProjectileSlot(Span<Projectile> projectiles, SyncProjectile proj)
        {
            // 首先查找匹配的现有弹幕
            for (int i = 0; i < 1000; i++)
            {
                ref readonly var p = ref projectiles[i];
                if (p.owner == proj.PlayerSlot && p.identity == proj.ProjSlot && p.active)
                    return i;
            }

            // 查找空闲槽位
            for (int i = 0; i < 1000; i++)
            {
                if (!projectiles[i].active)
                    return i;
            }

            // 使用最旧的弹幕槽位
            return Projectile.FindOldestProjectile();
        }

        private static void SetProjectileProperties(ref Projectile projectile, SyncProjectile proj)
        {
            if (!projectile.active || projectile.type != proj.ProjType)
            {
                projectile.SetDefaults(proj.ProjType);
                Netplay.Clients[proj.PlayerSlot].SpamProjectile += 1f;
            }

            // 批量设置属性
            projectile.identity = proj.ProjSlot;
            projectile.position = proj.Position.Get();
            projectile.velocity = proj.Velocity.Get();
            projectile.type = proj.ProjType;
            projectile.damage = proj.Damange;
            projectile.bannerIdToRespondTo = proj.BannerId;
            projectile.originalDamage = proj.OriginalDamage;
            projectile.knockBack = proj.Knockback;
            projectile.owner = proj.PlayerSlot;
            projectile.ai[0] = proj.AI1;
            projectile.ai[1] = proj.AI2;

            if (proj.UUID >= 0)
            {
                projectile.projUUID = proj.UUID;
                Main.projectileIdentity[proj.PlayerSlot, proj.UUID] =
                    Array.IndexOf(Main.projectile, projectile);
            }

            projectile.ProjectileFixDesperation();
        }

        // 清理资源的方法
        public static void Shutdown()
        {
            _cancellationTokenSource.Cancel();
            _projChannel.Writer.Complete();
            _processingSemaphore.Dispose();
            _cancellationTokenSource.Dispose();
        }

        public override bool OnGetPacket(BPlayer plr, SyncProjectile packet)
        {
            bool handled = base.OnGetPacket(plr, packet);

            if (BConfig.Instance.EnableProjQueue && !handled)
            {
                packet.PlayerSlot = plr.Index;

                // 使用TryWrite避免阻塞，如果队列满了就丢弃（背压控制）
                if (!_projChannel.Writer.TryWrite(packet))
                {
                    BLog.Warn("弹幕队列已满，丢弃数据包");
                }

                return true;
            }

            return handled;
        }

        public override bool OnSendPacket(BPlayer plr, SyncProjectile packet)
        {
            return base.OnSendPacket(plr, packet);
        }
    }

    public class DestroyProjectileHandler : PacketHandlerBase<KillProjectile>
    {
        private static readonly CancellationTokenSource _cancellationTokenSource = new();
        private static readonly Channel<KillProjectile> _projChannel =
            Channel.CreateUnbounded<KillProjectile>();

        [AutoInit]
        public static void InitProjQueue()
        {
            _ = Task.Run(ProcessProjLoopAsync, _cancellationTokenSource.Token);
        }

        private static async Task ProcessProjLoopAsync()
        {
            try
            {
                await foreach (var proj in _projChannel.Reader.ReadAllAsync(_cancellationTokenSource.Token))
                {
                    ProcessKillProjectile(proj);
                }
            }
            catch (OperationCanceledException) when (_cancellationTokenSource.Token.IsCancellationRequested)
            {
                // 优雅关闭
            }
            catch (Exception ex)
            {
                BLog.Error($"销毁弹幕处理循环异常: {ex}");
            }
        }

        private static void ProcessKillProjectile(KillProjectile proj)
        {
            try
            {
                var projectiles = Main.projectile.AsSpan();

                for (int i = 0; i < 1000; i++)
                {
                    ref readonly var p = ref projectiles[i];
                    if (p.owner == proj.PlayerSlot && p.identity == proj.ProjSlot && p.active)
                    {
                        Main.projectile[i].Kill();
                        break;
                    }
                }

                // 异步发送网络消息
                _ = Task.Run(() => NetMessage.TrySendData(29, -1, proj.PlayerSlot, null, proj.ProjSlot, proj.PlayerSlot));
            }
            catch (Exception ex)
            {
                BLog.Error($"处理弹幕销毁时发生错误: {ex}");
            }
        }

        public static void Shutdown()
        {
            _cancellationTokenSource.Cancel();
            _projChannel.Writer.Complete();
            _cancellationTokenSource.Dispose();
        }

        public override bool OnGetPacket(BPlayer plr, KillProjectile packet)
        {
            bool handled = base.OnGetPacket(plr, packet);

            if (BConfig.Instance.EnableProjQueue && !handled)
            {
                packet.PlayerSlot = plr.Index;

                if (!_projChannel.Writer.TryWrite(packet))
                {
                    BLog.Warn("弹幕销毁队列已满，丢弃数据包");
                }

                return true;
            }

            return handled;
        }

        public override bool OnSendPacket(BPlayer plr, KillProjectile packet)
        {
            return base.OnSendPacket(plr, packet);
        }
    }
}
