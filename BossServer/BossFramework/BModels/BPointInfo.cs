using BossFramework.DB;
using FreeSql;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BossFramework.BModels
{
    public class BPointInfo : UserConfigBase<BPointInfo>
    {
        public int Point { get; set; }
        public string FromReason { get; set; }
        public int TargetId { get; set; }
    }
}
