using System.Reflection;
using BossFramework;
using BossFramework.BCore;
using BossFramework.BModels;
using BossFramework.DB;
using FakeProvider;
using Microsoft.Xna.Framework;
using Terraria;
using TerrariaApi.Server;
using TShockAPI;

namespace PlotMarker
{
    [ApiVersion(2, 1)]
    public sealed class PlotMarker : TerrariaPlugin
    {
        public override string Name => GetType().Name;
        public override string Author => "MR.H, Megghy修改";
        public override string Description => "Marks plots for players and manages them.";
        public override Version Version => Assembly.GetExecutingAssembly().GetName().Version;

        internal static Configuration Config;

        public PlotMarker(Main game) : base(game) { }

        public override void Initialize()
        {
            ServerApi.Hooks.NetGetData.Register(this, OnGetData, 1000);
            ServerApi.Hooks.NetGreetPlayer.Register(this, OnGreet);
            ServerApi.Hooks.ServerLeave.Register(this, OnLeave);
            ServerApi.Hooks.GamePostInitialize.Register(this, OnPostInitialize, 100);
            ServerApi.Hooks.WorldSave.Register(this, (args) => { Task.Run(() => PlotManager.CurrentPlot?.Cells.Where(c => c.IsVisiable).ForEach(c => c.SaveCellData())); });

            SignRedirector.SignUpdate += OnUpdateSign;
            //SignRedirector.SignCreate += SignRedirector_SignCreate;
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                ServerApi.Hooks.NetGetData.Deregister(this, OnGetData);
                ServerApi.Hooks.NetGreetPlayer.Deregister(this, OnGreet);
                ServerApi.Hooks.ServerLeave.Deregister(this, OnLeave);
                ServerApi.Hooks.GamePostInitialize.Deregister(this, OnPostInitialize);

                SignRedirector.SignUpdate -= OnUpdateSign;
                //SignRedirector.SignCreate -= SignRedirector_SignCreate;
            }
            base.Dispose(disposing);
        }


        private static void OnLeave(LeaveEventArgs args)
        {
            var name = TShock.Players[args.Who]?.Name;
            if (string.IsNullOrWhiteSpace(name))
                return;

            var cells = PlotManager.GetCellsOfPlayer(name);
            foreach (var c in cells)
            {
                if (c.IsVisiable)
                    c.SaveCellData();
            }
        }

        private static void OnInitialize(EventArgs e)
        {

        }

        private static void OnPostInitialize(EventArgs args)
        {
            Config = Configuration.Read(Configuration.ConfigPath);
            Config.Write(Configuration.ConfigPath);

            Commands.ChatCommands.Add(new Command("pm.admin.areamanage", AreaManage, "areamanage", "属地区域", "am")
            {
                AllowServer = false,
                HelpText = "管理属地区域, 只限管理."
            });

            Commands.ChatCommands.Add(new Command("pm.player.getcell", MyPlot, "myplot", "属地", "mp")
            {
                AllowServer = false,
                HelpText = "管理玩家自己的属地区域."
            });

            Commands.ChatCommands.Add(new Command("pm.admin.cellmanage", CellManage, "cellmanage", "格子", "cm")
            {
                AllowServer = false,
                HelpText = "管理玩家属地区域, 只限管理."
            });

            PlotManager.Reload();

            if (PlotManager.CurrentPlot != null)
            {
                var rect = new Rectangle(PlotManager.CurrentPlot.X, PlotManager.CurrentPlot.Y, PlotManager.CurrentPlot.Width, PlotManager.CurrentPlot.Height);
                var count = SignRedirector.Signs.RemoveAll(s => rect.Contains(s.X, s.Y));
                if (count > 0)
                    TShock.Log.ConsoleInfo($"[PlotMarker] 忽略 {count} 个属地中的标牌");
            }
            //设置未隐藏的属地的信息
            PlotManager.CurrentPlot?.Cells?.ForEach(c =>
            {
                if (c.IsVisiable)
                {
                    c.RestoreCellTileData(false);
                    c.RegisteChestAndSign();
                    c.RestoreEntities(false);
                }
                else
                {
                    c.TileData = null;
                    c.Entities = null;
                }
            });

            FakeProviderAPI.World.ScanEntities(); //fakeprovider重新获取entity
        }

        private static void OnUpdateSign(BEventArgs.SignUpdateEventArgs args)
        {
            var plr = args.Player.TSPlayer;
            if (plr.GetCurrentCell() is { } cell)
            {
                if (!cell.CanEdit(plr) && !plr.HasPermission("pm.admin.updatesign"))
                {
                    args.Handled = true;
                    plr.SendInfoMessage($"你没有权限修改其他属地的牌子");
                }
            }
        }

        private static void OnGreet(GreetPlayerEventArgs args)
        {
            var player = TShock.Players[args.Who];
            PlayerInfo.GetInfo(player);
        }

        private static void OnGetData(GetDataEventArgs args)
        {
            if (args.Handled)
            {
                return;
            }

            var type = args.MsgID;

            var player = TShock.Players[args.Msg.whoAmI];
            if (player == null || !player.ConnectionAlive)
            {
                return;
            }
            using var stream = new MemoryStream(args.Msg.readBuffer, args.Index, args.Length - 1);
            args.Handled = Handlers.HandleGetData(type, player, stream);
        }
        private static void AreaManage(CommandArgs args)
        {
            var cmd = args.Parameters.Count > 0 ? args.Parameters[0].ToLower() : "help";
            var info = PlayerInfo.GetInfo(args.Player);

            switch (cmd)
            {
                case "点":
                case "point":
                    {
                        if (args.Parameters.Count != 2)
                        {
                            args.Player.SendErrorMessage("语法无效. 正确语法: /am point <1/2>");
                            return;
                        }

                        if (!byte.TryParse(args.Parameters[1], out var point) || point > 2 || point < 1)
                        {
                            args.Player.SendErrorMessage("选点无效. 正确: /am point <1/2>");
                            return;
                        }

                        info.Status = (PlayerInfo.PointStatus)point;
                        args.Player.SendInfoMessage("敲击物块以设定点 {0}", point);
                    }
                    break;
                case "定义":
                case "define":
                    {
                        if (args.Parameters.Count != 2)
                        {
                            args.Player.SendErrorMessage("语法无效. 正确语法: /am define <区域名>");
                            return;
                        }
                        if (info.X == -1 || info.Y == -1 || info.X2 == -1 || info.Y2 == -1)
                        {
                            args.Player.SendErrorMessage("你需要先选择区域.");
                            return;
                        }
                        if (PlotManager.AddPlot(info.X, info.Y, info.X2 - info.X, info.Y2 - info.Y,
                            args.Parameters[1], args.Player.Name,
                            Main.worldID, Config.PlotStyle))
                        {
                            args.Player.SendSuccessMessage("添加属地 {0} 完毕.", args.Parameters[1]);
                        }
                        else
                        {
                            args.Player.SendSuccessMessage("属地 {0} 已经存在, 请更换属地名后重试.", args.Parameters[1]);
                        }

                    }
                    break;
                case "updatepos":
                    if (PlotManager.CurrentPlot is null)
                    {
                        args.Player.SendErrorMessage($"还没属地");
                        return;
                    }
                    PlotManager.UpdateCellsPos(PlotManager.CurrentPlot);
                    break;
                case "删除":
                case "del":
                    {
                        if (args.Parameters.Count != 2)
                        {
                            args.Player.SendErrorMessage("语法无效. 正确语法: /am del <区域名>");
                            return;
                        }
                        var name = args.Parameters[1];
                        var plot = PlotManager.GetPlotByName(name);
                        if (plot == null)
                        {
                            args.Player.SendErrorMessage("未找到属地!");
                            return;
                        }
                        if (PlotManager.DelPlot(plot))
                        {
                            args.Player.SendSuccessMessage("成功删除属地.");
                            return;
                        }
                        args.Player.SendErrorMessage("删除属地失败!");
                    }
                    break;
                case "划分":
                case "mark":
                    {
                        if (args.Parameters.Count < 2 || args.Parameters.Count > 3)
                        {
                            args.Player.SendErrorMessage("语法无效. 正确语法: /am mark <区域名> [Clear:true/false]");
                            return;
                        }
                        var name = args.Parameters[1];
                        var plot = PlotManager.GetPlotByName(name);
                        if (plot == null)
                        {
                            args.Player.SendErrorMessage("未找到属地!");
                            return;
                        }
                        var clear = true;
                        if (args.Parameters.Count == 3)
                        {
                            switch (args.Parameters[2].ToLower())
                            {
                                case "true":
                                    break;
                                case "false":
                                    clear = false;
                                    break;
                                default:
                                    args.Player.SendErrorMessage("Clear属性值只能为 true/false");
                                    return;
                            }
                        }
                        plot.Generate(clear);
                        plot.Cells.Where(c => c.IsVisiable).ForEach(c => c.RestoreCellTileData());
                    }
                    break;
                case "信息":
                case "info":
                    {
                        if (args.Parameters.Count != 2)
                        {
                            args.Player.SendErrorMessage("语法无效. 正确语法: /am info <区域名>");
                            return;
                        }
                        var name = args.Parameters[1];
                        var plot = PlotManager.GetPlotByName(name);
                        if (plot == null)
                        {
                            args.Player.SendErrorMessage("未找到属地!");
                            return;
                        }

                        if (!PaginationTools.TryParsePageNumber(args.Parameters, 2, args.Player, out var pageNumber))
                        {
                            return;
                        }
                        var list = new List<string>
                        {
                            $" * 区域信息: {{{plot.X}, {plot.Y}, {plot.Width}, {plot.Height}}}",
                            $" * 格子信息: w={plot.CellWidth}, h={plot.CellHeight}, cur={plot.CellsPosition.Count}, used={plot.Cells.Count()}",
                            $" * 创建者名: {plot.Owner}"
                        };
                        PaginationTools.SendPage(args.Player, pageNumber, list,
                            new PaginationTools.Settings
                            {
                                HeaderFormat = "属地 " + plot.Name + " 说明 ({0}/{1}):",
                                FooterFormat = "键入 {0}pm info {1} {{0}} 以获取下一页列表.".SFormat(Commands.Specifier, plot.Name),
                                NothingToDisplayString = "当前没有说明."
                            });
                    }
                    break;
                case "列表":
                case "list":
                    {
                        if (!PaginationTools.TryParsePageNumber(args.Parameters, 1, args.Player, out var pageNumber))
                        {
                            return;
                        }

                        var plots = PlotManager.Plots.Select(p => p.Name);

                        PaginationTools.SendPage(args.Player, pageNumber, PaginationTools.BuildLinesFromTerms(plots),
                            new PaginationTools.Settings
                            {
                                HeaderFormat = "属地列表 ({0}/{1}):",
                                FooterFormat = "键入 {0}pm list {{0}} 以获取下一页列表.".SFormat(Commands.Specifier),
                                NothingToDisplayString = "当前没有属地."
                            });
                    }
                    break;
                case "重载":
                case "reload":
                    {
                        PlotManager.Reload();
                        Config = Configuration.Read(Configuration.ConfigPath);
                        Config.Write(Configuration.ConfigPath);
                        args.Player.SendSuccessMessage("重载完毕.");
                    }
                    break;
                case "帮助":
                case "help":
                    {
                        if (!PaginationTools.TryParsePageNumber(args.Parameters, 1, args.Player, out int pageNumber))
                        {
                            return;
                        }
                        var list = new List<string>
                        {
                            "point <1/2> - 选中点/区域",
                            "define <属地名> - 定义属地",
                            "del <属地名> - 删除属地",
                            "mark <属地名> - 在属地中生成格子",
                            "info <属地名> - 查看属地属性",
                            "list [页码] - 查看现有的属地",
                            "help [页码] - 获取帮助",
                            "reload - 载入数据库数据"
                        };
                        PaginationTools.SendPage(args.Player, pageNumber, list,
                            new PaginationTools.Settings
                            {
                                HeaderFormat = "属地管理子指令说明 ({0}/{1}):",
                                FooterFormat = "键入 {0}am help {{0}} 以获取下一页列表.".SFormat(Commands.Specifier),
                                NothingToDisplayString = "当前没有说明."
                            });
                    }
                    break;
                default:
                    {
                        args.Player.SendWarningMessage("子指令无效! 输入 {0} 获取帮助信息.",
                            TShock.Utils.ColorTag("/am help", Color.Cyan));
                    }
                    break;
            }
        }

        private static void MyPlot(CommandArgs args)
        {
            if (!args.Player.IsLoggedIn)
            {
                args.Player.SendErrorMessage("你未登录, 无法使用属地.");
                return;
            }

            var cmd = args.Parameters.Count > 0 ? args.Parameters[0].ToLower() : "help";
            var info = PlayerInfo.GetInfo(args.Player);

            switch (cmd)
            {
                case "获取":
                case "get":
                    {
                        if (args.Parameters.Count != 1)
                        {
                            args.Player.SendErrorMessage("语法无效. 正确语法: {0}", TShock.Utils.ColorTag("/属地 获取", Color.Cyan));
                            return;
                        }

                        var count = PlotManager.GetTotalCells(args.Player.Account.Name);
                        var max = args.Player.GetMaxCells();
                        if (max != -1 && count > max)
                        {
                            args.Player.SendErrorMessage("你无法获取更多属地. (你当前有{0}个/最多{1}个)", count, max);
                            return;
                        }

                        if (PlotManager.CreateNewCell(args.Player) is { } newCell)
                        {
                            newCell.Visiable(args.Player);
                            args.Player.Teleport(newCell.Center.X * 16, newCell.Center.Y * 16);
                        }
                        else
                        {
                            args.Player.SendErrorMessage("获取失败, 请联系管理员", count, max);
                        }
                    }
                    break;
                case "允许":
                case "添加":
                case "allow":
                    {
                        if (args.Parameters.Count < 2)
                        {
                            args.Player.SendErrorMessage("语法无效. 正确语法: {0}", TShock.Utils.ColorTag("/属地 允许 <玩家名>", Color.Cyan));
                            return;
                        }

                        var count = PlotManager.GetTotalCells(args.Player.Account.Name);
                        switch (count)
                        {
                            case 0:
                                args.Player.SendErrorMessage("你没有属地!");
                                break;
                            case 1:
                                var cell = PlotManager.GetOnlyCellOfPlayer(args.Player.Account.Name);
                                if (cell == null)
                                {
                                    args.Player.SendErrorMessage("载入属地失败! 请联系管理 (不唯一或缺少)");
                                    return;
                                }
                                InternalSetUser(args.Parameters, args.Player, cell, true);
                                break;
                            default:
                                if (count > 1)
                                {
                                    info.Status = PlayerInfo.PointStatus.Delegate;
                                    info.OnGetPoint = InternalSetUserWithPoint;
                                    args.Player.SendInfoMessage("在你的属地内放置物块来添加用户.");
                                }
                                break;
                        }

                        void InternalSetUserWithPoint(int x, int y, TSPlayer receiver) => InternalSetUser(args.Parameters, receiver, PlotManager.GetCellByPosition(x, y), true);
                    }
                    break;
                case "禁止":
                case "删除":
                case "disallow":
                    {
                        if (args.Parameters.Count < 2)
                        {
                            args.Player.SendErrorMessage("语法无效. 正确语法: {0}", TShock.Utils.ColorTag("/属地 禁止 <玩家名>", Color.Cyan));
                            return;
                        }

                        var count = PlotManager.GetTotalCells(args.Player.Account.Name);
                        switch (count)
                        {
                            case 0:
                                args.Player.SendErrorMessage("你没有属地!");
                                break;
                            case 1:
                                var cell = PlotManager.GetOnlyCellOfPlayer(args.Player.Account.Name);
                                if (cell == null)
                                {
                                    args.Player.SendErrorMessage("载入属地失败! 请联系管理 (不唯一或缺少)");
                                    return;
                                }
                                InternalSetUser(args.Parameters, args.Player, cell, false);
                                break;
                            default:
                                if (count > 1)
                                {
                                    info.Status = PlayerInfo.PointStatus.Delegate;
                                    info.OnGetPoint = InternalSetUserWithPoint;
                                    args.Player.SendInfoMessage("在你的属地内放置物块来移除用户.");
                                }
                                break;
                        }

                        void InternalSetUserWithPoint(int x, int y, TSPlayer receiver) => InternalSetUser(args.Parameters, receiver, PlotManager.GetCellByPosition(x, y), false);
                    }
                    break;
                case "信息":
                case "查询":
                case "info":
                    {
                        if (args.Parameters.Count > 1)
                        {
                            if (int.TryParse(args.Parameters[1], out var cellIndex))
                            {
                                if (PlotManager.CurrentPlot.Cells.FirstOrDefault(c => c.Id == cellIndex) is { } tempCellInfo)
                                {
                                    args.Player.SendInfoMessage(tempCellInfo.GetInfo());
                                }
                                else
                                    args.Player.SendErrorMessage($"未找到id为 {cellIndex} 的子属地");
                            }
                            else
                                args.Player.SendErrorMessage("语法无效. 正确语法: /gm info <指定区域ID>");
                        }
                        else
                        {
                            if (args.Player.GetCurrentCell() is { } tempCellInfo2)
                                args.Player.SendInfoMessage(tempCellInfo2.GetInfo());
                            else
                                args.Player.SendErrorMessage("你未处于某个子属地内");
                        }
                    }
                    break;
                case "save":
                    if (args.Player.HasPermission(permission: "pm.admin.save"))
                    {
                        if (args.Player.GetCurrentCell() is { } cell)
                        {
                            args.Player.SendInfoMessage($"保存{(cell.SaveCellData() ? "成功" : "失败")}");
                        }
                    }
                    break;
                case "goto":
                case "前往":
                    {
                        Cell[] cells = args.Player.HasPermission("pm.admin.gotoeverywhere") && args.Parameters.Count > 1
                        ? PlotManager.CurrentPlot.Cells.ToArray()
                        : PlotManager.GetCellsOfPlayer(args.Player.Name);
                        if (cells.Any())
                        {
                            if (args.Parameters.Count > 1)
                            {
                                if (int.TryParse(args.Parameters[1], out var cellIndex))
                                {
                                    if (cells.FirstOrDefault(c => c.Id == cellIndex) is { } cell)
                                    {
                                        GotoCell(args.Player, cell);
                                    }
                                    else
                                        args.Player.SendErrorMessage($"未找到Id为 {cellIndex} 的属地, 或者它不属于你");
                                }
                                else
                                    args.Player.SendErrorMessage($"无效的属地编号: {args.Parameters[1]}");
                            }
                            else
                                GotoCell(args.Player, cells.First());
                        }
                        else
                            args.Player.SendInfoMessage($"未找到任何, 请输入 {"/mp get".Color("7FDFDE")} 来获取属地");
                        void GotoCell(TSPlayer plr, Cell cell)
                        {
                            plr.SendInfoMessage($"正在前往属地 [{cell.Id}]");
                            if (!cell.IsVisiable && !cell.Visiable(plr))
                                return;
                            plr.Teleport(cell.AbsloteSpawnX * 16, (cell.AbsloteSpawnY - 3) * 16);
                            plr.SendSuccessMessage($"已传送至属地 [{cell.Id}]");
                        }
                    }
                    break;
                case "list":
                case "列表":
                    {
                        args.Player.SendInfoMessage(PlotManager.GetCellsOfPlayer(args.Player.Name) is { Length: > 0 } listCell
                            ? string.Join("\r\n", listCell.Select(c => c.GetInfo()))
                            : "你尚未获取属地");
                    }
                    break;
                case "帮助":
                case "help":
                    {
                        if (!PaginationTools.TryParsePageNumber(args.Parameters, 1, args.Player, out var pageNumber))
                        {
                            return;
                        }
                        var list = new List<string>
                        {
                            "获取(get) - 获取一块属地",
                            "允许(allow/添加) <玩家名> - 给自己的属地增加协助者 ",
                            "禁止(disallow/删除) <玩家名> - 移除协助者 ",
                            "信息(info/查询) - 查看当前点坐标所在属地的信息 ",
                            "列表(list) - 查看自己所拥有的属地",
                            "前往(goto) <属地ID>- 前往指定id的属地, 不指定则会默认前往自己的第一块属地",
                            "帮助 [页码] - 获取帮助 (help/帮助)"
                        };
                        PaginationTools.SendPage(args.Player, pageNumber, list,
                            new PaginationTools.Settings
                            {
                                HeaderFormat = "玩家属地子指令说明 ({0}/{1}):",
                                FooterFormat = "键入 {0}属地 帮助 {{0}} 以获取下一页列表.".SFormat(Commands.Specifier),
                                NothingToDisplayString = "当前没有说明."
                            });
                    }
                    break;
                default:
                    {
                        args.Player.SendWarningMessage("子指令无效! 输入 {0} 获取帮助信息.",
                            TShock.Utils.ColorTag("/属地 帮助", Color.Cyan));
                    }
                    break;
            }

            void InternalSetUser(IEnumerable<string> parameters, TSPlayer player, Cell target, bool allow)
            {
                var playerName = string.Join(" ", parameters.Skip(1));
                var user = TShock.UserAccounts.GetUserAccountByName(playerName);

                if (user == null)
                {
                    player.SendErrorMessage("玩家 " + playerName + " 未找到");
                    return;
                }

                if (target != null)
                {
                    if (target.Owner != player.Account.Name && !player.HasPermission("pm.admin.editall"))
                    {
                        player.SendErrorMessage("你不是该属地的主人.");
                        return;
                    }

                    if (allow)
                    {
                        if (PlotManager.AddCellUser(target, user))
                            player.SendInfoMessage("添加用户 " + playerName + " 完毕.");
                        else
                            player.SendErrorMessage("添加用户时出现问题.");
                    }
                    else
                    {
                        if (PlotManager.RemoveCellUser(target, user))
                            player.SendInfoMessage("移除用户 " + playerName + " 完毕.");
                        else
                            player.SendErrorMessage("移除用户时出现问题.");
                    }
                }
                else
                {
                    player.SendErrorMessage("该点坐标不在属地内.");
                }
            }
        }

        private static void CellManage(CommandArgs args)
        {
            var cmd = args.Parameters.Count > 0 ? args.Parameters[0].ToLower() : "help";
            var info = PlayerInfo.GetInfo(args.Player);

            switch (cmd)
            {
                case "list":
                    {
                        if (!PaginationTools.TryParsePageNumber(args.Parameters, 1, args.Player, out var pageNumber))
                        {
                            return;
                        }

                        var cells = PlotManager.CurrentPlot.Cells.Select(p => p.GetInfo());

                        PaginationTools.SendPage(args.Player, pageNumber, PaginationTools.BuildLinesFromTerms(cells),
                            new PaginationTools.Settings
                            {
                                HeaderFormat = "子属地列表 ({0}/{1}):",
                                FooterFormat = "键入 {0}pm list {{0}} 以获取下一页列表.".SFormat(Commands.Specifier),
                                NothingToDisplayString = "当前没有子属地."
                            });
                    }
                    break;
                case "fuck":
                case "艹":
                    {
                        if (!args.Player.HasPermission("pm.admin.fuckcell"))
                        {
                            args.Player.SendErrorMessage("无权限执行.");
                            return;
                        }
                        if (args.Parameters.Count > 1)
                        {
                            if (int.TryParse(args.Parameters[1], out var cellIndex))
                            {
                                if (PlotManager.CurrentPlot.Cells.FirstOrDefault(c => c.Id == cellIndex) is { } tempCellInfo)
                                {
                                    InternalFuckCell(args.Player, tempCellInfo);
                                }
                                else
                                    args.Player.SendErrorMessage($"未找到id为 {cellIndex} 的子区域");
                            }
                            else
                                args.Player.SendErrorMessage("语法无效. 正确语法: /gm fuck <指定区域ID>");
                        }
                        else
                        {
                            if (args.Player.GetCurrentCell() is { } tempCellInfo2)
                                InternalFuckCell(args.Player, tempCellInfo2);
                            else
                                args.Player.SendErrorMessage("你未处于某个子属地内");
                        }

                        void InternalFuckCell(TSPlayer player, Cell cell)
                        {
                            if (args.Parameters.Exists(p => p.ToLower() == "true"))
                            {
                                PlotManager.FuckCell(cell);
                                player.SendSuccessMessage("愉悦, 艹完了.");
                            }
                            else
                            {
                                player.SendInfoMessage($"你确定要清空 {cell.Owner} 的属地 [{cell.Id}] 吗? 确定要清空请输入/cm hide {cell.Id} true");
                            }
                        }
                    }
                    break;
                case "del":
                    {
                        if (!args.Player.HasPermission("pm.admin.delcell"))
                        {
                            args.Player.SendErrorMessage("无权限执行.");
                            return;
                        }
                        if (args.Parameters.Count > 1)
                        {
                            if (int.TryParse(args.Parameters[1], out var cellIndex))
                            {
                                if (PlotManager.CurrentPlot.Cells.FirstOrDefault(c => c.Id == cellIndex) is { } tempCellInfo)
                                {
                                    InternalDelCell(args.Player, tempCellInfo);
                                }
                                else
                                    args.Player.SendErrorMessage($"未找到id为 {cellIndex} 的子区域");
                            }
                            else
                                args.Player.SendErrorMessage("语法无效. 正确语法: /gm del <指定区域ID>");
                        }
                        else
                        {
                            if (args.Player.GetCurrentCell() is { } tempCellInfo2)
                                InternalDelCell(args.Player, tempCellInfo2);
                            else
                                args.Player.SendErrorMessage("你未处于某个子属地内");
                        }

                        void InternalDelCell(TSPlayer player, Cell cell)
                        {
                            if (args.Parameters.Exists(p => p.ToLower() == "true"))
                            {
                                PlotManager.FuckCell(cell);
                                player.SendSuccessMessage("指定属地已被删除");
                            }
                            else
                            {
                                player.SendInfoMessage($"你确定要删除 {cell.Owner} 的属地 [{cell.Id}] 吗? 确定要删除请输入/cm del {cell.Id} true");
                            }
                        }
                    }
                    break;
                case "hide":
                case "隐藏":
                    {
                        Cell[] hideCells = args.Player.HasPermission("pm.admin.hide")
                        ? PlotManager.CurrentPlot.Cells.ToArray()
                        : PlotManager.GetCellsOfPlayer(args.Player.Name);
                        if (hideCells.Any())
                        {
                            if (args.Parameters.Count > 1)
                            {
                                if (int.TryParse(args.Parameters[1], out var cellIndex))
                                {
                                    if (hideCells.FirstOrDefault(c => c.Id == cellIndex) is { } cell)
                                    {
                                        HideCell(args.Player, cell);
                                    }
                                    else
                                        args.Player.SendErrorMessage($"未找到Id为 {cellIndex} 的属地, 或者它不属于你");
                                }
                                else
                                    args.Player.SendErrorMessage($"无效的属地编号: {args.Parameters[1]}");
                            }
                            else
                                args.Player.SendErrorMessage($"未指定属地编号");
                        }
                        else
                            args.Player.SendInfoMessage($"没有可用属地对象");
                    }
                    void HideCell(TSPlayer plr, Cell cell)
                    {
                        if (cell.IsVisiable)
                        {
                            cell.Invisiable();
                            args.Player.SendSuccessMessage($"已隐藏属地 [{cell.Id}]");
                        }
                        else
                            args.Player.SendInfoMessage($"属地 [{cell.Id}] 未处于显示状态");
                    }
                    break;
                case "show":
                    Cell[] showCells = args.Player.HasPermission("pm.admin.show")
                        ? PlotManager.CurrentPlot.Cells.ToArray()
                        : PlotManager.GetCellsOfPlayer(args.Player.Name);
                    if (showCells.Any())
                    {
                        if (args.Parameters.Count > 1)
                        {
                            if (int.TryParse(args.Parameters[1], out var cellIndex))
                            {
                                if (showCells.FirstOrDefault(c => c.Id == cellIndex) is { } cell)
                                {
                                    cell.Visiable(args.Player);
                                    args.Player.SendSuccessMessage($"已显示 {cell.Id}");
                                }
                                else
                                    args.Player.SendErrorMessage($"未找到Id为 {cellIndex} 的属地, 或者它不属于你");
                            }
                            else
                                args.Player.SendErrorMessage($"无效的属地编号: {args.Parameters[1]}");
                        }
                        else
                            args.Player.SendErrorMessage($"未指定属地编号");
                    }
                    else
                        args.Player.SendInfoMessage($"没有可用属地对象");
                    break;
                case "here":
                    if (args.Parameters.Count > 0)
                    {
                        if (args.Player.GetCurrentCell() is { } here)
                        {
                            if (int.TryParse(args.Parameters[1], out var cellIndex))
                            {
                                if (PlotManager.CurrentPlot.Cells.FirstOrDefault(c => c.Id == cellIndex) is { } tempCellInfo)
                                {
                                    args.Player.SendInfoMessage(tempCellInfo.GetInfo());
                                }
                                else
                                    args.Player.SendErrorMessage($"未找到id为 {cellIndex} 的子区域");
                            }
                            else
                                args.Player.SendErrorMessage("语法无效. 正确语法: /gm here <指定区域ID>");
                        }
                        else
                            args.Player.SendErrorMessage("你未处于某个属地内");
                    }
                    else
                        args.Player.SendErrorMessage("语法无效. 正确语法: /gm here <指定区域ID>");
                    break;
                case "info":
                    {
                        if (args.Parameters.Count > 1)
                        {
                            if (int.TryParse(args.Parameters[1], out var cellIndex))
                            {
                                if (PlotManager.CurrentPlot.Cells.FirstOrDefault(c => c.Id == cellIndex) is { } tempCellInfo)
                                {
                                    args.Player.SendInfoMessage(tempCellInfo.GetInfo());
                                }
                                else
                                    args.Player.SendErrorMessage($"未找到id为 {cellIndex} 的子区域");
                            }
                            else
                                args.Player.SendErrorMessage("语法无效. 正确语法: /gm info <指定区域ID>");
                        }
                        else
                        {
                            if (args.Player.GetCurrentCell() is { } tempCellInfo2)
                                args.Player.SendInfoMessage(tempCellInfo2.GetInfo());
                            else
                                args.Player.SendErrorMessage("你未处于某个子属地内");
                        }
                    }
                    break;
                case "chown":
                    {
                        if (args.Parameters.Count < 2)
                        {
                            args.Player.SendErrorMessage("语法无效. 正确语法: {0}", TShock.Utils.ColorTag("/cm chown <账户名>", Color.Cyan));
                            return;
                        }

                        info.Status = PlayerInfo.PointStatus.Delegate;
                        info.OnGetPoint = InternalChownWithPoint;
                        args.Player.SendInfoMessage("在你的属地内敲击或放置物块来更换领主.");

                        void InternalChownWithPoint(int x, int y, TSPlayer receiver) => InternalChown(args.Parameters, receiver, PlotManager.GetCellByPosition(x, y));

                        void InternalChown(IEnumerable<string> parameters, TSPlayer player, Cell target)
                        {
                            var playerName = string.Join(" ", parameters.Skip(1));
                            var user = TShock.UserAccounts.GetUserAccountByName(playerName);

                            if (user == null)
                            {
                                player.SendErrorMessage("用户 " + playerName + " 未找到");
                                return;
                            }

                            if (target != null)
                            {
                                if (target.Owner != player.Account.Name && !player.HasPermission("pm.admin.editall"))
                                {
                                    player.SendErrorMessage("你不是该属地的主人.");
                                    return;
                                }

                                PlotManager.ChangeOwner(target, user);
                                player.SendSuccessMessage("完成更换主人.");
                            }
                            else
                            {
                                player.SendErrorMessage("该点坐标不在属地内.");
                            }
                        }
                    }
                    break;
                case "redraw":
                    if (!args.Player.HasPermission("pm.admin.redraw"))
                    {
                        args.Player.SendErrorMessage("你没有使用此命令的权限");
                        return;
                    }
                    if (PlotManager.CurrentPlot is null)
                        args.Player.SendErrorMessage($"尚未生成属地");
                    else
                    {
                        PlotManager.CurrentPlot.Cells.ForEach(c => c.RestoreCellTileData(false));
                        TileHelper.ResetSection(PlotManager.CurrentPlot.X, PlotManager.CurrentPlot.Y, PlotManager.CurrentPlot.Width, PlotManager.CurrentPlot.Height);
                        args.Player.SendSuccessMessage("完成");
                    }
                    break;
                case "fix":
                    {
                        var count = 0;
                        PlotManager.CurrentPlot?.Cells.OrderByDescending(c => c.LastAccess).ForEach(c =>
                        {
                            if (c.UsingCellPositionIndex.Any())
                            {
                                if (PlotManager.CurrentPlot?.Cells.Where(temp => temp != c
                                    && temp.UsingCellPositionIndex.SequenceEqual(c.UsingCellPositionIndex)).ToArray() is { Length: > 0 } shouldHide)
                                {
                                    shouldHide.ForEach(c =>
                                    {
                                        c.UsingCellPositionIndex.Clear();
                                        DBTools.SQL.Update<Cell>(c)
                                            .Set(c => c.UsingCellPositionIndex, c.UsingCellPositionIndex)
                                            .ExecuteAffrows();
                                        BLog.Info($"隐藏重叠的属地 [{c.Id}]");
                                        count++;
                                    });
                                    c.ReDraw();
                                }
                            }
                        });
                        args.Player.SendInfoMessage($"修复 {count} 个重叠的属地");
                    }
                    break;
                case "help":
                    {
                        if (!PaginationTools.TryParsePageNumber(args.Parameters, 1, args.Player, out var pageNumber))
                        {
                            return;
                        }
                        var list = new List<string>
                        {
                            "info <id> - 获取指定区域信息, 未填写id则默认为玩家所处属地",
                            "show <id> - 展示指定区域, 替换掉最久没人使用过的属地",
                            "here <id> - 展示指定区域, 替换点当前所在属地",
                            "del <id> - 隐藏指定区域, 未填写id则默认为玩家所处属地",
                            "fix - 修复因为某些原因重叠而无法使用的属地",
                            "fuck <id> - 重置指定区域, 未填写id则默认为玩家所处属地",
                            "chown <id> - 更改指定区域所有者(未完成)",
                            "del <id> - 删除指定区域, 未填写id则默认为玩家所处属地",
                            "help [页码] - 获取帮助"
                        };
                        PaginationTools.SendPage(args.Player, pageNumber, list,
                            new PaginationTools.Settings
                            {
                                HeaderFormat = "玩家属地管理子指令说明 ({0}/{1}):",
                                FooterFormat = "键入 {0}cm help {{0}} 以获取下一页列表.".SFormat(Commands.Specifier),
                                NothingToDisplayString = "当前没有说明."
                            });
                    }
                    break;
                default:
                    {
                        args.Player.SendWarningMessage("子指令无效! 输入 {0} 获取帮助信息.",
                            TShock.Utils.ColorTag("/cm help", Color.Cyan));
                    }
                    break;
            }
        }

        public static bool BlockModify(TSPlayer player, int tileX, int tileY)
        {
            if (!BlockModify_ShouldStop(player, tileX, tileY))
            {
                return false;
            }
            var info = PlayerInfo.GetInfo(player);
            if (DateTime.Now.Ticks / TimeSpan.TicksPerMillisecond - info.BPm > 2000)
            {
                if (player.HasPermission("pm.build.everywhere"))
                    player.SendErrorMessage("该区域尚未生成属地");
                else
                    player.SendErrorMessage("该属地被保护, 无法更改物块.");
                info.BPm = DateTime.Now.Ticks / TimeSpan.TicksPerMillisecond;
            }

            return true;
        }

        private static bool BlockModify_ShouldStop(TSPlayer player, int tileX, int tileY)
        {
            if (!player.IsLoggedIn)
            {
                return true;
            }
            if (PlotManager.CurrentPlot is null || !PlotManager.CurrentPlot.Contains(tileX, tileY))
                return false;
            else if (PlotManager.CurrentPlot.FindCell(tileX, tileY) is { } cell)
            {
                if (cell.CanEdit(player))
                {
                    cell.LastAccess = DateTime.Now;
                    return false;
                }
            }
            else if (PlotManager.CurrentPlot.IsWall(tileX, tileY) == true)
            {
                return !player.HasPermission("pm.build.wall");
            }
            return true;
        }
    }
}
