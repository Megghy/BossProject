using System.Collections.Concurrent;
using System.Diagnostics;
using System.Runtime;
using BossFramework.BAttributes;

namespace BossFramework.BNet
{
    /// <summary>
    /// 网络性能监控器
    /// </summary>
    public static class NetworkPerformanceMonitor
    {
        private static readonly ConcurrentDictionary<string, PerformanceCounter> _counters = new();
        private static Timer _reportTimer;
        private static readonly object _lockObject = new();

        [AutoInit]
        internal static void Init()
        {
            _reportTimer = new Timer(ReportStats, null, TimeSpan.FromSeconds(30), TimeSpan.FromSeconds(30));
        }

        /// <summary>
        /// 性能计数器
        /// </summary>
        public class PerformanceCounter
        {
            private long _count;
            private long _totalTime;
            private long _minTime = long.MaxValue;
            private long _maxTime;
            private readonly Stopwatch _stopwatch = new();

            public long Count => _count;
            public double AverageTime => _count > 0 ? (double)_totalTime / _count : 0;
            public long MinTime => _minTime == long.MaxValue ? 0 : _minTime;
            public long MaxTime => _maxTime;

            public void Record(long timeMs)
            {
                Interlocked.Increment(ref _count);
                Interlocked.Add(ref _totalTime, timeMs);

                // 更新最小值
                long currentMin;
                do
                {
                    currentMin = _minTime;
                    if (timeMs >= currentMin) break;
                } while (Interlocked.CompareExchange(ref _minTime, timeMs, currentMin) != currentMin);

                // 更新最大值
                long currentMax;
                do
                {
                    currentMax = _maxTime;
                    if (timeMs <= currentMax) break;
                } while (Interlocked.CompareExchange(ref _maxTime, timeMs, currentMax) != currentMax);
            }

            public IDisposable StartTiming()
            {
                return new TimingScope(this);
            }

            private class TimingScope : IDisposable
            {
                private readonly PerformanceCounter _counter;
                private readonly Stopwatch _stopwatch;

                public TimingScope(PerformanceCounter counter)
                {
                    _counter = counter;
                    _stopwatch = Stopwatch.StartNew();
                }

                public void Dispose()
                {
                    _stopwatch.Stop();
                    _counter.Record(_stopwatch.ElapsedMilliseconds);
                }
            }
        }

        /// <summary>
        /// 获取或创建性能计数器
        /// </summary>
        /// <param name="name">计数器名称</param>
        /// <returns>性能计数器</returns>
        public static PerformanceCounter GetCounter(string name)
        {
            return _counters.GetOrAdd(name, _ => new PerformanceCounter());
        }

        /// <summary>
        /// 记录操作性能
        /// </summary>
        /// <param name="name">操作名称</param>
        /// <param name="timeMs">耗时（毫秒）</param>
        public static void RecordOperation(string name, long timeMs)
        {
            GetCounter(name).Record(timeMs);
        }

        /// <summary>
        /// 开始计时操作
        /// </summary>
        /// <param name="name">操作名称</param>
        /// <returns>可释放的计时作用域</returns>
        public static IDisposable StartTiming(string name)
        {
            return GetCounter(name).StartTiming();
        }

        /// <summary>
        /// 报告性能统计
        /// </summary>
        private static void ReportStats(object state)
        {
            if (!BConfig.Instance.DebugInfo) return;

            lock (_lockObject)
            {
                BLog.Info("=== 网络性能统计 ===");

                foreach (var kvp in _counters)
                {
                    var counter = kvp.Value;
                    if (counter.Count > 0)
                    {
                        BLog.Info($"{kvp.Key}: 次数={counter.Count}, " +
                                $"平均={counter.AverageTime:F2}ms, " +
                                $"最小={counter.MinTime}ms, " +
                                $"最大={counter.MaxTime}ms");
                    }
                }

                // 报告GC信息
                var gen0 = GC.CollectionCount(0);
                var gen1 = GC.CollectionCount(1);
                var gen2 = GC.CollectionCount(2);
                var memory = GC.GetTotalMemory(false) / 1024 / 1024;

                BLog.Info($"GC统计: Gen0={gen0}, Gen1={gen1}, Gen2={gen2}, 内存={memory}MB");
                BLog.Info("==================");
            }
        }

        /// <summary>
        /// 重置所有计数器
        /// </summary>
        public static void Reset()
        {
            _counters.Clear();
        }

        /// <summary>
        /// 获取所有计数器的快照
        /// </summary>
        /// <returns>计数器快照字典</returns>
        public static Dictionary<string, (long Count, double AvgTime, long MinTime, long MaxTime)> GetSnapshot()
        {
            var result = new Dictionary<string, (long, double, long, long)>();

            foreach (var kvp in _counters)
            {
                var counter = kvp.Value;
                result[kvp.Key] = (counter.Count, counter.AverageTime, counter.MinTime, counter.MaxTime);
            }

            return result;
        }

        /// <summary>
        /// 清理资源
        /// </summary>
        public static void Dispose()
        {
            _reportTimer?.Dispose();
            _counters.Clear();
        }
    }

    /// <summary>
    /// 网络配置优化
    /// </summary>
    public static class NetworkOptimizer
    {
        /// <summary>
        /// 应用网络优化设置
        /// </summary>
        public static void ApplyOptimizations()
        {
            // 设置线程池最小线程数
            int workerThreads = Math.Max(Environment.ProcessorCount, 8);
            int completionPortThreads = Math.Max(Environment.ProcessorCount, 8);

            ThreadPool.SetMinThreads(workerThreads, completionPortThreads);

            // 设置GC延迟模式（适用于服务器应用）
            if (GCSettings.IsServerGC)
            {
                GCSettings.LatencyMode = GCLatencyMode.SustainedLowLatency;
            }

            BLog.Info($"网络优化已应用: 工作线程={workerThreads}, IO线程={completionPortThreads}");
        }

        /// <summary>
        /// 获取推荐的批处理大小
        /// </summary>
        /// <param name="currentLoad">当前负载（0-1.0）</param>
        /// <returns>推荐的批处理大小</returns>
        public static int GetRecommendedBatchSize(double currentLoad)
        {
            // 根据负载动态调整批处理大小
            return currentLoad switch
            {
                < 0.3 => 20,  // 低负载
                < 0.7 => 50,  // 中等负载
                _ => 100      // 高负载
            };
        }

        /// <summary>
        /// 获取推荐的工作线程数
        /// </summary>
        /// <returns>推荐的工作线程数</returns>
        public static int GetRecommendedWorkerCount()
        {
            var coreCount = Environment.ProcessorCount;

            // 对于I/O密集型操作，可以使用更多线程
            return Math.Max(2, Math.Min(coreCount * 2, 16));
        }
    }
}