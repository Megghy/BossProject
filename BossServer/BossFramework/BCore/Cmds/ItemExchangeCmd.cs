using BossFramework.BAttributes;
using BossFramework.BInterfaces;
using BossFramework.BModels;
using BossFramework.BModules;
using System.Collections.Generic;
using System.Linq;
using TrProtocol;
using TrProtocol.Packets;
using TShockAPI;

namespace BossFramework.BCore.Cmds
{
    public class ItemExchangeCmd : BaseCommand
    {
        public override string[] Names { get; } = { "ie" };
        public static void Exchange(SubCommandArgs args)
        {
            if (args.Any())
            {
                if (ItemExchangeConfig.Instance.Recipes.FirstOrDefault(r => r.Name.IsSimilarWith(args[0])) is { } recipe)
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
                    if (ItemExchangeConfig.Instance.Recipes.FirstOrDefault(r => r.Name.IsSimilarWith(args[1])) is { } recipe)
                        InternalExchange(plr, recipe);
                    else
                        args.SendErrorMsg($"未找到名为 {args[1]} 的交换规则");
                else
                    args.SendErrorMsg($"未找到名为 {args[0]} 的玩家");
            }
            else
                args.SendErrorMsg($"格式错误. /ie exchangeother <玩家名> <配方名>");
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
            var money = new MoneyInfo(plrData.inventory.Count(i => i.NetId == 74),
                plrData.inventory.Count(i => i.NetId == 73),
                plrData.inventory.Count(i => i.NetId == 72),
                plrData.inventory.Count(i => i.NetId == 71));
            Dictionary<int, (NetItem item, bool beEmpty)> items = new();
            recipe.RequireItem.ForEach(item =>
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
            if (items.Count == recipe.RequireItem.Length)
            {
                if (money.TotalCoin >= recipe.RequireCoin && plr.TrPlayer.BuyItem(recipe.RequireCoin))
                {
                    var packets = new List<Packet>();
                    items.ForEach(i =>
                    {

                    });
                    packets.SendPacketsToAll();
                    plrData.CopyCharacter(plr.TsPlayer);
                    plrData.inventory.ForEach((i, slot) =>
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
                         else
                            packets.Add(new SyncEquipment()
                            {
                                ItemSlot = (short)slot,
                                ItemType = (short)i.NetId,
                                PlayerSlot = plr.Index,
                                Prefix = i.PrefixId,
                                Stack = (short)i.Stack
                            });
                    });
                    packets.SendPacketsToAll();

                    recipe.GiveItem.ForEach(i => plr.TsPlayer.GiveItem(i.NetId, i.Stack, i.PrefixId));
                    recipe.ExcuteCommands.ForEach(c => Commands.HandleCommand(plr.TsPlayer, c));

                    plr.SendSuccessMsg($"成功兑换配方 {recipe.Name}");
                }
                else
                    plr.SendErrorMsg($"金钱不足. 改配方所需 [{MoneyInfo.GetFromCopper(recipe.RequireCoin)}], 你的金钱为 [{money}]");
            }
            else
                plr.SendErrorMsg($"以下所需物品未在你的背包中找到: \r\n[{string.Join(", ", recipe.RequireItem.Where(i => !items.Any(existItem => existItem.Value.item.Equals(i))).Select(i => TShock.Utils.ItemTag(i)))}]");
        }
    }
}
