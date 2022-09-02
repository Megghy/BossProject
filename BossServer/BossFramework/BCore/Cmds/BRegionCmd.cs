using BossFramework.BAttributes;
using BossFramework.BInterfaces;
using BossFramework.BModels;
using System.Linq;

namespace BossFramework.BCore.Cmds
{
    public class BRegionCmd : BaseCommand
    {
        public override string[] Names { get; } = { "bregion", "br" };

        [SubCommand("addtag", Permission = "boss.bregion.admin.addtag")]
        [SubCommand("deltag", Permission = "boss.bregion.admin.deltag")]
        public static void SetTag(SubCommandArgs args)
        {
            if (args.Count() > 1)
            {
                if (BRegionSystem.FindBRegionByName(args[0]) is { } region)
                {
                    if (args.SubCommandName == "addtag")
                    {
                        if (!region.Tags.Contains(args[1]))
                        {
                            region.AddTag(args[1].ToLower(), false);
                            args.SendSuccessMsg($"成功添加标签 {args[1]}");
                        }
                        else
                            args.SendInfoMsg($"标签 {args[1]} 已存在于区域 {region.Name} 中");
                    }
                    else
                    {
                        if (region.Tags.Contains(args[1]))
                        {
                            region.DelTag(args[1]);
                            args.SendSuccessMsg($"已移除标签 {args[1]}");
                        }
                        else
                            args.SendInfoMsg($"标签 {args[1]} 不存在于区域 {region.Name} 中");
                    }
                }
                else
                    args.SendErrorMsg($"未找到名为 {args[0]} 的区域");
            }
            else
                args.SendErrorMsg($"格式错误. /bregion(br) {args.SubCommandName} <区域名> <标签名>");
        }
        [SubCommand("listtag", Permission = "boss.bregion.admin.listtag")]
        public static void List(SubCommandArgs args)
        {
            if (args.Any())
            {
                if (BRegionSystem.FindBRegionByName(args[0]) is { } region)
                    args.SendInfoMsg("Tags: " + string.Join(", ", region.Tags));
                else
                    args.SendErrorMsg($"未找到名为 {args[0]} 的区域");
            }
            else
                args.SendErrorMsg($"格式错误. /bregion(br) listtag <区域名>");
        }

        [SubCommand("setparent", Permission = "boss.bregion.admin.setparent")]
        public static void SetParent(SubCommandArgs args)
        {
            if (args.Count() > 1)
            {
                if (BRegionSystem.FindBRegionByName(args[0]) is { } region)
                {
                    if (BRegionSystem.FindBRegionByName(args[0]) is { } parentRegion)
                    {
                        parentRegion.AddChild(region);
                        args.SendSuccessMsg($"已设置 {region.Name} 为 {parentRegion.Name}, 此父区域共有以下子区域: {string.Join(", ", parentRegion.ChildRegion.Select(r => r.Name))}");
                    }
                    else
                        args.SendErrorMsg($"未找到名为 {args[1]} 的父区域");
                }
                else
                    args.SendErrorMsg($"未找到名为 {args[0]} 的区域");
            }
            else
                args.SendErrorMsg($"格式错误. /bregion(br) {args.SubCommandName} <区域名> <标签名>");
        }
        [SubCommand("delparent", Permission = "boss.bregion.admin.delparent")]
        public static void DelParent(SubCommandArgs args)
        {
            if (args.Count() > 1)
            {
                if (BRegionSystem.FindBRegionByName(args[0]) is { } region)
                {
                    region.SetParent(null);
                    args.SendSuccessMsg($"已移除 {region.Name} 的父区域设置");
                }
                else
                    args.SendErrorMsg($"未找到名为 {args[0]} 的区域");
            }
            else
                args.SendErrorMsg($"格式错误. /bregion(br) {args.SubCommandName} <区域名> <标签名>");
        }
    }
}
