using System.IO;
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
    }
}
