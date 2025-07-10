using BossFramework.BInterfaces;

namespace BossFramework
{
    public class BConfig : BaseConfig<BConfig>
    {
        protected override string FilePath => Path.Combine(BInfo.FilePath, "Config.json");

        public bool FastLoadWorld { get; set; } = true;
        public bool DebugInfo { get; set; } = true;
        public int SignRefreshRadius { get; set; } = 200;
        public int ChangWeaponDelay { get; set; } = 10;
        public bool DisableSpawnBroadcast { get; set; } = true;
        public bool EnableNewSocketService { get; set; } = false;
        public bool EnableProjRedirect { get; set; } = false;
        public bool EnableProjQueue { get; set; } = true;
        public bool EnableChatAboveHead { get; set; } = false;

        // 网络性能优化配置
        public int NetworkSwitchThreshold { get; set; } = 20; // 当在线人数超过此值，新连接将使用高吞吐量模式。-1为禁用（总是用低延迟模式）。
        public int ProjectileBatchSize { get; set; } = 50;
        public int ProjectileWorkerThreads { get; set; } = 0; // 0表示自动检测
        public int MaxSendQueueSize { get; set; } = 10000;
        public int NetworkStatsReportInterval { get; set; } = 30; // 秒
        public bool EnableNetworkOptimizations { get; set; } = true;
    }
}
