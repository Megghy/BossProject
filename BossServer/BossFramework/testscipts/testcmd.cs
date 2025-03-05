using System.Collections.Generic;
using System.IO;
using System.Linq;
using BossFramework;
using BossFramework.BAttributes;
using BossFramework.BInterfaces;
using BossFramework.BModels;
using TrProtocol;
using TrProtocol.Packets;
using TShockAPI;

public class ItemExchangeCmd : BaseCommand
{
    public struct ExchangeRecipe
    {
        public ExchangeRecipe() { }
        public string Name { get; set; } = "";
        public bool Notice { get; set; } = true;
        /// <summary>
        /// 需要的钱, 换算为铜币
        /// </summary>
        public int RequireCoin { get; set; } = -1;
        /// <summary>
        /// 需要的物品
        /// </summary>
        public NetItem[] RequireItem { get; set; } = [];
        /// <summary>
        /// 给的物品
        /// </summary>
        public NetItem[] GiveItem { get; set; } = [];
        /// <summary>
        /// 换了之后执行的命令
        /// </summary>
        public string[] ExcuteCommands { get; set; } = [];
    }
    public class ItemExchangeConfig_Internal : BaseConfig<ItemExchangeConfig_Internal>
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
    public override string[] Names { get; } = { "ie" };
    public static void Exchange(SubCommandArgs args)
    {
        if (args.Any())
        {
            if (ItemExchangeConfig_Internal.Instance.Recipes.FirstOrDefault(r => r.Name.ToLower() == args[0].ToLower()) is { } recipe)
                InternalExchange(args.Player, recipe);
            else
                args.SendErrorMsg($"未找到名为 {args[0]} 的交换规则");
        }
        else
            args.SendErrorMsg($"格式错误. /ie exchange <配方名>");
    }
    [NeedPermission("boss.exchange.admin.other")]
    public static void ExchangeOther(SubCommandArgs args)
    {
        if (args.Any())
        {
            if (BInfo.OnlinePlayers.FirstOrDefault(p => p.Name.IsSimilarWith(args[0])) is { } plr)
                if (ItemExchangeConfig_Internal.Instance.Recipes.FirstOrDefault(r => r.Name.IsSimilarWith(args[1])) is { } recipe)
                    InternalExchange(plr, recipe);
                else
                    args.SendErrorMsg($"未找到名为 {args[1]} 的交换规则");
            else
                args.SendErrorMsg($"未找到名为 {args[0]} 的玩家");
        }
        else
            args.SendErrorMsg($"格式错误. /ie exchangeother <玩家名> <配方名>");
    }
    public static void List(SubCommandArgs args)
    {
        args.SendInfoMsg($"可用配方:\r\n{string.Join("\r\n", ItemExchangeConfig_Internal.Instance.Recipes.Select(r => $"{r.Name} [{MoneyInfo.GetFromCopper(r.RequireCoin)}] 所需物品: {string.Join(' ', r.RequireItem.Select(i => TShock.Utils.ItemTag(i)))}"))}");
    }
    record MoneyInfo(long Platinum, long Gold, long Silver, long Copper)
    {
        public long TotalCoin
            => (Platinum * 1000000) + (Gold * 10000) + (Silver * 100) + Copper;

        public static MoneyInfo GetFromCopper(int copper)
        {
            var p = copper / 1000000;
            copper -= p * 1000000;
            var g = copper / 10000;
            copper -= g * 10000;
            var s = copper / 100;
            copper -= s * 100;
            return new MoneyInfo(p, g, s, copper);
        }
        public override string ToString()
            => $"{Platinum} 铂, {Gold} 金, {Silver} 银, {Copper} 铜";
    }
    private static void InternalExchange(BPlayer plr, ExchangeRecipe recipe)
    {
        var plrData = new PlayerData(plr.TsPlayer);
        plrData.CopyCharacter(plr.TsPlayer);
        var money = new MoneyInfo(plrData.inventory.Where(i => i.NetId == 74).Sum(i => i.Stack),
            plrData.inventory.Where(i => i.NetId == 73).Sum(i => i.Stack),
            plrData.inventory.Where(i => i.NetId == 72).Sum(i => i.Stack),
            plrData.inventory.Where(i => i.NetId == 71).Sum(i => i.Stack));
        Dictionary<int, (NetItem item, bool beEmpty)> items = new();
        recipe.RequireItem?.ForEach(item =>
        {
            int slot = 0;
            foreach (var i in plr.TrPlayer.inventory)
            {
                if (i?.type == item.NetId && (item.PrefixId == 0 || item.PrefixId == i?.prefix) && i?.stack >= item.Stack)
                {
                    items.Add(slot, (item, i?.stack - item.Stack <= 0));
                    break;
                }
                slot++;
            }
        });
        var blankSlot = plr.TrPlayer.inventory.Take(50).Count(i => i == null || i.type == 0) + items.Count(i => i.Value.beEmpty);
        if (recipe.GiveItem?.Any() == true)
            foreach (var item in recipe.GiveItem)
            {
                if (blankSlot > 0)
                    blankSlot--;
                else if (plr.TrPlayer.inventory.FirstOrDefault(i => i?.type == item.NetId) is { } existItem)
                {
                    if (existItem.stack + item.Stack > existItem.maxStack)
                        blankSlot++;
                }
                else
                {
                    plr.SendErrorMsg($"背包剩余空间不足以放下物品. 所需空间: {recipe.GiveItem.Length}");
                    return;
                }
            }
        if (items.Count == (recipe.RequireItem?.Length ?? 0))
        {
            if (money.TotalCoin >= recipe.RequireCoin && plr.TrPlayer.BuyItem(recipe.RequireCoin))
            {
                var packets = new List<Packet>();
                plrData.CopyCharacter(plr.TsPlayer);
                plrData.inventory?.ForEachWithIndex((i, slot) =>
                {
                    try
                    {
                        if (items.TryGetValue(slot, out var itemInfo))
                        {
                            if (itemInfo.beEmpty)
                                plr.TrPlayer.inventory[slot].SetDefaults();
                            else
                                plr.TrPlayer.inventory[slot].stack -= itemInfo.item.Stack;
                            packets.Add(new SyncEquipment()
                            {
                                ItemSlot = (short)slot,
                                ItemType = (short)(itemInfo.beEmpty ? 0 : itemInfo.item.NetId),
                                PlayerSlot = plr.Index,
                                Prefix = plr.TrPlayer.inventory[slot].prefix,
                                Stack = (short)plr.TrPlayer.inventory[slot].stack
                            });
                        }
                        else if (slot < 59 && i.NetId is 71 or 72 or 73 or 74 or 0)
                            packets.Add(new SyncEquipment()
                            {
                                ItemSlot = (short)slot,
                                ItemType = (short)i.NetId,
                                PlayerSlot = plr.Index,
                                Prefix = i.PrefixId,
                                Stack = (short)i.Stack
                            });
                    }
                    catch (System.Exception ex)
                    {
                        BLog.Error($"slot: {slot}, ex: {ex}");
                    }
                });
                plr.SendPacket(BUtils.GetCurrentWorldData(true));
                packets.SendPacketsToAll();
                if (!Terraria.Main.ServerSideCharacter)
                    plr.SendPacket(BUtils.GetCurrentWorldData(false));

                recipe.GiveItem?.ForEach(i => plr.TsPlayer.GiveItem(i.NetId, i.Stack, i.PrefixId));
                recipe.ExcuteCommands?.ForEach(c => Commands.HandleCommand(plr.TsPlayer, c));

                if (recipe.Notice)
                    plr.SendSuccessMsg($"成功兑换配方 {recipe.Name}");
            }
            else
                plr.SendErrorMsg($"金钱不足. 需要 [{MoneyInfo.GetFromCopper(recipe.RequireCoin)}], 你的金钱为 [{money}]");
        }
        else
            plr.SendErrorMsg($"以下所需物品未在你的背包中找到: \r\n[{string.Join(", ", recipe.RequireItem?.Where(i => !items.Any(existItem => existItem.Value.item.Equals(i))).Select(i => TShock.Utils.ItemTag(i)))}]");
    }
}
