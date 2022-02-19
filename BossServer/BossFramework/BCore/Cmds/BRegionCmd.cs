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
                    if(args.SubCommandName == "addtag")
                    {
                        if (!region.TagsName.Contains(args[1]))
                        {
                            if (BRegionSystem.RegionTags.FirstOrDefault(t => t.Name.IsSimilarWith(args[1])) is { } tag)
                            {
                                var newTag = tag.CreateInstance(region);
                                region.AddTag(newTag, false);
                                args.SendSuccessMsg($"成功添加标签 {tag.Name}");
                            }
                            else
                                args.SendInfoMsg($"未找到名为 {args[1]} 的标签");
                        }
                        else
                            args.SendInfoMsg($"标签 {args[1]} 已存在于区域 {region.Name} 中");
                    }
                    else
                    {
                        if (region.TagsName.Contains(args[1]))
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
            args.SendInfoMsg(string.Join("\r\n", BRegionSystem.RegionTags.Select(r => $"{r.Name}: {r.Description}")));
        }
    }
}
