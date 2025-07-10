using BossFramework.BInterfaces;
using TShockAPI;

namespace BossFramework.BModules
{
    public struct ExchangeRecipe
    {
        public ExchangeRecipe() { }
        public string Name { get; set; } = "";
        /// <summary>
        /// 需要的钱, 换算为铜币
        /// </summary>
        public int RequireCoin { get; set; } = -1;
        /// <summary>
        /// 需要的物品
        /// </summary>
        public NetItem[] RequireItem { get; set; } = Array.Empty<NetItem>();
        /// <summary>
        /// 给的物品
        /// </summary>
        public NetItem[] GiveItem { get; set; } = Array.Empty<NetItem>();
        /// <summary>
        /// 换了之后执行的命令
        /// </summary>
        public string[] ExcuteCommands { get; set; } = Array.Empty<string>();
    }
    public class ItemExchangeConfig : BaseConfig<ItemExchangeConfig>
    {
        protected override string FilePath => Path.Combine(BInfo.FilePath, "ItemExchange.json");
        public ExchangeRecipe[] Recipes { get; set; } = {
            new ExchangeRecipe
            {
                ExcuteCommands = new string[] { "/help" },
                GiveItem = new NetItem[] { new(1500, 1, 0) },
                Name = "test",
                RequireCoin = 100,
                RequireItem = new NetItem[]
                {
                    new NetItem(757, 1, 0)
                }
            }
        };
    }
}
