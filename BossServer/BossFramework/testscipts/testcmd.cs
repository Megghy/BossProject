using System.Linq;
using BossFramework.BAttributes;
using BossFramework.BInterfaces;
using BossFramework.BModels;
using TShockAPI;

public class RegulationCmd : BaseCommand
{
    public override string[] Names => new[] { "regulation" };

    public const string IgnoreDisablePerm = "ignore.disable.all";
    public const string DisableTag = "disable.all";

    public const string RecordItemChangeTag = "record.item";
    public const string RecordProjChangeTag = "record.proj";

    [SubCommand("disable", Permission = "regulation.disable", Description = "区域内全员禁用")]
    public static void DisableRegion(SubCommandArgs args)
    {
        var region = GetRegionFromArg(args);
        if (region is null) return;
        if (!region.Tags.Contains(DisableTag))
        {
            region.Tags.Add(DisableTag);
        }
        region.GetPlayers().ForEach(p =>
        {
            if (!p.TsPlayer.HasPermission(IgnoreDisablePerm))
            {
                p.TsPlayer.Disable();
            }
        });
        args.Player.SendSuccessMsg($"已禁用区域: {region.Name}");
    }

    [SubCommand("enable", Permission = "regulation.enable", Description = "区域内全员解除禁用")]
    public static void EnableRegion(SubCommandArgs args)
    {
        var region = GetRegionFromArg(args);
        if (region is null) return;
        if (region.Tags.Contains(DisableTag))
        {
            region.Tags.RemoveAll(t => t == DisableTag);
            region.GetPlayers().ForEach(p =>
            {
                p.TsPlayer.Disable();
            });
        }
        args.Player.SendSuccessMsg($"已恢复区域: {region.Name}");
    }

    [SubCommand("dp", Permission = "regulation.disable", Description = "禁用玩家")]
    public static void DisablePlayer(SubCommandArgs args)
    {
        var target = GetPlayerFromArg(args);
        if (target is null) return;
        target.Disable();
        args.Player.SendSuccessMsg($"已禁用玩家: {target.Name}");
    }

    [SubCommand("ep", Permission = "regulation.enable", Description = "解除禁用玩家")]
    public static void EnablePlayer(SubCommandArgs args)
    {
        var target = GetPlayerFromArg(args);
        if (target is null) return;
        target.Disable();
        args.Player.SendSuccessMsg($"已恢复玩家: {target.Name}");
    }

    private static BRegion? GetRegionFromArg(SubCommandArgs args)
    {
        BRegion? region;
        if (args.Count() == 0)
        {
            region = args.Player.CurrentRegion;
        }
        else if (args.Count() == 1)
        {
            var regionName = args[0];
            region = BossFramework.BCore.BRegionSystem.FindBRegionByName(regionName);
        }
        else
        {
            args.Player.SendErrorMsg("参数数量错误");
            return null;
        }

        if (region is null)
        {
            args.Player.SendErrorMsg("无效区域");
            return null;
        }

        return region;
    }

    private static TSPlayer? GetPlayerFromArg(SubCommandArgs args)
    {
        TSPlayer? player = null;
        if (args.Count() == 0)
        {
            args.Player.SendErrorMsg("参数数量错误");
        }
        else if (args.Count() == 1)
        {
            var results = TSPlayer.FindByNameOrID(args[0]);
            if (results.Count() == 1) return results[0];
            args.Player.SendMultipleMatchError(results.Select(p => p.Name));
        }

        if (player is null)
        {
            args.Player.SendErrorMsg("找不到指定玩家");
            return null;
        }

        return player;
    }
}