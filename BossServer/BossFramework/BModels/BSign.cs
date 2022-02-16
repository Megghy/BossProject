using BossFramework.DB;
using FreeSql.DataAnnotations;

namespace BossFramework.BModels
{
    public class BSign : UserConfigBase<BSign>
    {
        /// <summary>
        /// 所属世界
        /// </summary>
        public long WorldId { get; set; }
        /// <summary>
        /// x坐标
        /// </summary>
        public int X { get; set; }
        /// <summary>
        /// y坐标
        /// </summary>
        public int Y { get; set; }
        /// <summary>
        /// 标牌文本
        /// </summary>
        [Column(DbType = "text")]
        public string Text { get; set; }
        /// <summary>
        /// 创建者
        /// </summary>
        public int Owner { get; set; }
        /// <summary>
        /// 最后修改者
        /// </summary>
        public int LastUpdateUser { get; set; }

        public bool Contains(int tileX, int tileY)
            => (X == tileX || X + 1 == tileX) && (Y == tileY || Y + 1 == tileY);

        public override string ToString()
            => $"{X} - {Y}: {Text}";
    }
}
