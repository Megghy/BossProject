using BossFramework.DB;

namespace BossFramework.BModels
{
    public class BPointInfo : UserConfigBase<BPointInfo>
    {
        public int Point { get; set; }
        public string FromReason { get; set; }
        public int TargetId { get; set; }
    }
}
