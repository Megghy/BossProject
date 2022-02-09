using System.Reflection;
using Terraria;
using TerrariaApi.Server;
using TShockAPI;
using TShockAPI.DB;
using TShockAPI.Hooks;

namespace TeleportRestriction
{
    [ApiVersion(2, 1)]
    public class TeleportRestriction : TerrariaPlugin
    {
        public const string BypassPermission = "teleportres.admin.bypass";

        public override string Name => Assembly.GetExecutingAssembly().GetName().Name;

        public override string Author => "MistZZT";

        public override Version Version => Assembly.GetExecutingAssembly().GetName().Version;

        public override string Description => "限制传送";

        public TeleportRestriction(Main game) : base(game) { }

        internal TrRegionManager Trm;

        public override void Initialize()
        {
            ServerApi.Hooks.GameInitialize.Register(this, OnInit, -2000);
            ServerApi.Hooks.GamePostInitialize.Register(this, OnPostInit, -2000);
            RegionHooks.RegionDeleted += OnRegionDeleted;
            GetDataHandlers.Teleport += OnTeleport;
        }



        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                ServerApi.Hooks.GameInitialize.Deregister(this, OnInit);
                ServerApi.Hooks.GamePostInitialize.Deregister(this, OnPostInit);
                RegionHooks.RegionDeleted -= OnRegionDeleted;
                GetDataHandlers.Teleport -= OnTeleport;
            }
            base.Dispose(disposing);
        }
        private void OnRegionDeleted(RegionHooks.RegionDeletedEventArgs args)
        {
            Trm = new TrRegionManager(TShock.DB);
            Trm.DeleteRtRegion(args.Region.ID);
        }
        private void OnTeleport(object sender, GetDataHandlers.TeleportEventArgs e)
        {
            var flag = (BitsByte)e.Flag;

            if (flag[0]) // npc teleport
            {
                return;
            }

            var player = TShock.Players.ElementAtOrDefault(e.ID);
            if (player?.Active != true)
                return;

            if (player.HasPermission(BypassPermission))
                return;

            if (!flag[1] && Trm.IsInsideRoD(player, (int)e.X / 16, (int)e.Y / 16))
                return;

            if (Trm.ShouldRes(player.TileX, player.TileY, (int)e.X / 16, (int)e.Y / 16))
            {
                player.SendErrorMessage("无法传送.");
                player.Teleport(player.TPlayer.position.X, player.TPlayer.position.Y);
                player.Disable("禁止传送区域内传送", DisableFlags.None);
                e.Handled = true;
            }
        }

        private void OnPostInit(EventArgs args)
        {
            Trm = new TrRegionManager(TShock.DB);
            Trm.LoadRegions();
        }

        private void OnInit(EventArgs args)
        {
            var index = Commands.ChatCommands.FindIndex(cmd => cmd.HasAlias("tp"));

            Commands.ChatCommands.RemoveAll(cmd => cmd.HasAlias("tp"));

            Commands.ChatCommands.Insert(index, new Command(Permissions.tp, Teleport, "tp", "传")
            {
                AllowServer = false,
                HelpText = "传送到另外的玩家位置."
            });
            Commands.ChatCommands.Add(new Command("teleportres.manage", TrManage, "tpres"));
        }

        // 烂到了一种境界
        private void Teleport(CommandArgs args)
        {
            if (args.Parameters.Count != 1 && args.Parameters.Count != 2)
            {
                args.Player.SendErrorMessage("语法无效! 正确语法: {0}tp <玩家名>{1}",
                    Commands.Specifier, args.Player.HasPermission(Permissions.tpothers) ? " [玩家2]" : "");
                return;
            }

            if (args.Parameters.Count == 1)
            {
                var players = TSPlayer.FindByNameOrID(args.Parameters[0]);
                if (players.Count == 0)
                    args.Player.SendErrorMessage("指定玩家无效!");
                else if (players.Count > 1)
                    args.Player.SendMultipleMatchError(players.Select(p => p.Name));
                else
                {
                    var target = players[0];
                    if (!target.TPAllow && !args.Player.HasPermission(Permissions.tpoverride))
                    {
                        args.Player.SendErrorMessage("{0} 已经禁止别人传送至其位置.", target.Name);
                        return;
                    }
                    if (!Trm.ShouldRes(args.Player, target))
                    {
                        if (args.Player.Teleport(target.TPlayer.position.X, target.TPlayer.position.Y))
                        {
                            args.Player.SendSuccessMessage("传送至 {0}.", target.Name);
                            if (!args.Player.HasPermission(Permissions.tpsilent))
                                target.SendInfoMessage("{0} 传送至你所在的位置.", args.Player.Name);
                        }
                    }
                }
            }
            else
            {
                if (!args.Player.HasPermission(Permissions.tpothers))
                {
                    args.Player.SendErrorMessage("缺少执行该指令的权限.");
                    return;
                }

                var players1 = TSPlayer.FindByNameOrID(args.Parameters[0]);
                var players2 = TSPlayer.FindByNameOrID(args.Parameters[1]);

                if (players2.Count == 0)
                    args.Player.SendErrorMessage("指定玩家无效!");
                else if (players2.Count > 1)
                    args.Player.SendMultipleMatchError(players2.Select(p => p.Name));
                else if (players1.Count == 0)
                {
                    if (args.Parameters[0] == "*") // 不管这种情况
                    {
                        if (!args.Player.HasPermission(Permissions.tpallothers))
                        {
                            args.Player.SendErrorMessage("缺少执行该指令的权限.");
                            return;
                        }

                        var target = players2[0];
                        foreach (var source in TShock.Players.Where(p => p != null && p != args.Player))
                        {
                            if (!target.TPAllow && !args.Player.HasPermission(Permissions.tpoverride))
                                continue;
                            if (source.Teleport(target.TPlayer.position.X, target.TPlayer.position.Y))
                            {
                                if (args.Player != source)
                                {
                                    if (args.Player.HasPermission(Permissions.tpsilent))
                                        source.SendSuccessMessage("你被传送至 {0}.", target.Name);
                                    else
                                        source.SendSuccessMessage("{0} 传送你到 {1} 的位置.", args.Player.Name, target.Name);
                                }
                                if (args.Player != target)
                                {
                                    if (args.Player.HasPermission(Permissions.tpsilent))
                                        target.SendInfoMessage("{0} 被传送到你的位置.", source.Name);
                                    if (!args.Player.HasPermission(Permissions.tpsilent))
                                        target.SendInfoMessage("{0} 传送了 {1} 至你所在地.", args.Player.Name, source.Name);
                                }
                            }
                        }
                        args.Player.SendSuccessMessage("传送所有玩家至 {0}.", target.Name);
                    }
                    else
                        args.Player.SendErrorMessage("指定玩家无效!");
                }
                else if (players1.Count > 1)
                    args.Player.SendMultipleMatchError(players1.Select(p => p.Name));
                else
                {
                    var source = players1[0];
                    if (!source.TPAllow && !args.Player.HasPermission(Permissions.tpoverride))
                    {
                        args.Player.SendErrorMessage("{0} 禁止了玩家传送到其所在位置.", source.Name);
                        return;
                    }
                    var target = players2[0];
                    if (!target.TPAllow && !args.Player.HasPermission(Permissions.tpoverride))
                    {
                        args.Player.SendErrorMessage("{0} 禁止了玩家传送到其所在位置.", target.Name);
                        return;
                    }
                    if (!Trm.ShouldRes(source, target))
                    {
                        args.Player.SendErrorMessage("目标玩家在禁止传送区域内.");
                        args.Player.SendSuccessMessage("传送了玩家 {0} 至 {1} 的位置.", source.Name, target.Name);
                        if (source.Teleport(target.TPlayer.position.X, target.TPlayer.position.Y))
                        {
                            if (args.Player != source)
                            {
                                if (args.Player.HasPermission(Permissions.tpsilent))
                                    source.SendSuccessMessage("你被传送至 {0} 所在位置.", target.Name);
                                else
                                    source.SendSuccessMessage("{0} 传送你至 {1} 所在的位置.", args.Player.Name, target.Name);
                            }
                            if (args.Player != target)
                            {
                                if (args.Player.HasPermission(Permissions.tpsilent))
                                    target.SendInfoMessage("{0} 被传送到你所在的位置.", source.Name);
                                if (!args.Player.HasPermission(Permissions.tpsilent))
                                    target.SendInfoMessage("{0} 传送了 {1} 至你所在的位置.", args.Player.Name, source.Name);
                            }
                        }
                    }
                }
            }
        }

        private void TrManage(CommandArgs args)
        {
            var cmd = args.Parameters.ElementAtOrDefault(0)?.ToUpperInvariant() ?? "HELP";

            var status = args.Parameters.Count >= 3 ?
                string.Equals(args.Parameters[2], "true", StringComparison.OrdinalIgnoreCase) ? true :
                string.Equals(args.Parameters[2], "false", StringComparison.OrdinalIgnoreCase) ? false : (bool?)null
                : null;
            var regionName = args.Parameters.ElementAtOrDefault(1);
            Region region = null;

            switch (cmd)
            {
                case "TPTOREGION":
                case "TPOUT":
                case "WARP":
                case "INSIDEROD":
                    if (string.IsNullOrWhiteSpace(regionName))
                    {
                        args.Player.SendErrorMessage("区域名无效!");
                        return;
                    }
                    if (!status.HasValue)
                    {
                        args.Player.SendErrorMessage("语法无效! 正确语法: /tpres {0} <区域名> <true/false>", cmd.ToLowerInvariant());
                        return;
                    }
                    region = TShock.Regions.GetRegionByName(regionName);
                    if (region == null)
                    {
                        args.Player.SendErrorMessage("区域名无效!");
                        return;
                    }
                    break;
                case "REMOVE":
                    if (string.IsNullOrWhiteSpace(regionName))
                    {
                        args.Player.SendErrorMessage("区域名无效!");
                        return;
                    }
                    region = TShock.Regions.GetRegionByName(regionName);
                    if (region == null)
                    {
                        args.Player.SendErrorMessage("区域名无效!");
                        return;
                    }
                    break;
            }

            switch (cmd)
            {
                case "TPTOREGION":
                    var tr = Trm.CheckExist(region);
                    // ReSharper disable once PossibleInvalidOperationException
                    tr.AllowTpToRegion = status.Value; // 上面已经检测过了
                    Trm.Update(tr);
                    args.Player.SendSuccessMessage("{0}了传送至区域.", status.Value ? "允许" : "禁止");
                    break;
                case "TPOUT":
                    tr = Trm.CheckExist(region);
                    // ReSharper disable once PossibleInvalidOperationException
                    tr.AllowTpOut = status.Value;
                    Trm.Update(tr);
                    args.Player.SendSuccessMessage("{0}了传送出区域.", status.Value ? "允许" : "禁止");
                    break;
                case "WARP":
                    tr = Trm.CheckExist(region);
                    // ReSharper disable once PossibleInvalidOperationException
                    tr.AllowWarp = status.Value;
                    Trm.Update(tr);
                    args.Player.SendSuccessMessage("{0}了跳跃至区域.", status.Value ? "允许" : "禁止");
                    break;
                case "INSIDEROD":
                    tr = Trm.CheckExist(region);
                    // ReSharper disable once PossibleInvalidOperationException
                    tr.AllowInsideRoD = status.Value;
                    Trm.Update(tr);
                    args.Player.SendSuccessMessage("{0}了区域内传送法杖.", status.Value ? "允许" : "禁止");
                    break;
                case "REMOVE":
                    // ReSharper disable once PossibleNullReferenceException
                    Trm.Remove(region.ID);
                    args.Player.SendSuccessMessage("移除区域设定完毕.");
                    break;
                case "LIST":
                    args.Player.SendErrorMessage("不支持该功能.");
                    return;
                case "HELP":
                    #region help
                    int pageNumber;
                    if (!PaginationTools.TryParsePageNumber(args.Parameters, 1, args.Player, out pageNumber))
                        return;
                    var help = new[]
                    {
                        "tptoregion <区域名> <true/false> - - 玩家传送至区域内玩家",
                        "tpout <区域名> <true/false> - - 区域内玩家传送至区域外玩家",
                        "warp <区域名> <true/false> - - 玩家跳跃至区域内",
                        "insiderod <区域名> <true/false> - - 区域内传送法杖",
                        "remove <区域名> - - 删除区域内一切设定",
                        "list [页码] - - 显示所有区域",
                        "help [页码] - - 显示子指令帮助"
                    };
                    PaginationTools.SendPage(args.Player, pageNumber, help,
                        new PaginationTools.Settings
                        {
                            HeaderFormat = "传送限制指令帮助 ({0}/{1}):",
                            FooterFormat = "键入 {0}tpres help {{0}} 以获取下一页传送限制帮助.".SFormat(Commands.Specifier),
                            NothingToDisplayString = "当前没有可用帮助."
                        });
                    #endregion
                    break;
                default:
                    args.Player.SendErrorMessage("语法无效! 键入 /tpres help 查看帮助.");
                    return;
            }
        }
    }
}
