using BossFramework.DB;
using FreeSql.DataAnnotations;
using System.Linq;
using TrProtocol.Models;

namespace BossFramework.BModels
{
    public class BChest : UserConfigBase<BChest>
    {
        public override void Init()
        {
            if (Items?.Length < 40)
            {
                Items = new ItemData[40];
                40.ForEach(i =>
                {
                    Items[i] = new();
                });
            }
        }
        public override string ToString()
        {
            return $"[{X}, {Y}] {Name} [{Items.Count(i => i?.ItemID != 0)}/40]";
        }
        public long WorldId { get; set; }
        public short X { get; set; }
        public short Y { get; set; }
        public string Name { get; set; }
        [JsonMap]
        public ItemData[] Items { get; set; }
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
    }
}
