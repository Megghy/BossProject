using BossFramework.BInterfaces;
using System.IO;

namespace BossFramework
{
    public class BConfig : BaseConfig<BConfig>
    {
        protected override string FilePath => Path.Combine(BInfo.FilePath, "Config.json");

        public bool FastLoadWorld { get; set; } = true;
        public bool DebugInfo { get; set; } = true;
        public int SignRefreshRadius { get; set; } = 200;
        public int ChangWeaponDelay { get; set; } = 10;
        public bool EnableNewSocketService { get; set; } = false;
    }
}
