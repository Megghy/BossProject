using BossFramework.DB;

namespace BossFramework.BModels
{
    public class BPointInfo : DBStructBase<BPointInfo>
    {
        public int Point { get; set; }
        public string FromReason { get; set; }
        public int TargetId { get; set; }
    }
}
