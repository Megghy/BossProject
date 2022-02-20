using BossFramework;
using BossFramework.BNet;
using Philosophyz.Hooks;
using System.Reflection;
using Terraria;
using TerrariaApi.Server;
using TrProtocol.Packets;
using TShockAPI;
using TShockAPI.Hooks;
using static BossFramework.BModels.BEventArgs;

namespace Philosophyz
{
    [ApiVersion(2, 1)]
    public class Philosophyz : TerrariaPlugin
    {
        private const bool DefaultFakeSscStatus = false;

        private const double DefaultCheckTime = 1.5d;

        public override string Name => Assembly.GetExecutingAssembly().GetName().Name;

        public override string Author => "MistZZT";

        public override string Description => "Dark";

        public override Version Version => Assembly.GetExecutingAssembly().GetName().Version;

        public Philosophyz(Main game) : base(game)
        {
            Order = 0; // 最早
        }

        internal PzRegionManager PzRegions;

        public override void Initialize()
        {
            ServerApi.Hooks.GameInitialize.Register(this, OnInit);
            ServerApi.Hooks.GamePostInitialize.Register(this, OnPostInit);
            ServerApi.Hooks.GameUpdate.Register(this, OnUpdate, 9000);

            RegionHooks.RegionDeleted += OnRegionDeleted;

            PacketHandler.RegisteSendPacketHandler(PacketTypes.WorldInfo, OnSendWorldInfo);

            ServerApi.Hooks.NetSendData.Register(this, OnOtapiSendData);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                ServerApi.Hooks.GameInitialize.Deregister(this, OnInit);
                ServerApi.Hooks.GamePostInitialize.Deregister(this, OnPostInit);

                RegionHooks.RegionDeleted -= OnRegionDeleted;

                PacketHandler.DeregistePacketHandler(OnSendWorldInfo);

                ServerApi.Hooks.NetSendData.Deregister(this, OnOtapiSendData);
            }
            base.Dispose(disposing);
        }

        private DateTime _lastCheck = DateTime.UtcNow;

        private void OnUpdate(EventArgs args)
        {
            if ((DateTime.UtcNow - _lastCheck).TotalSeconds < DefaultCheckTime)
            {
                return;
            }

            foreach (var player in TShock.Players.Where(p => p?.Active == true))
            {
                var info = PlayerInfo.GetPlayerInfo(player);
                var oldRegion = info.CurrentRegion;
                info.CurrentRegion = TShock.Regions.GetTopRegion(TShock.Regions.InAreaRegion(player.TileX, player.TileY));

                if (oldRegion == info.CurrentRegion)
                    continue;

                var shouldInvokeLeave = true;

                // 若是pz区域，则更换模式；不需要在离开区域时再次复原或保存备份。
                if (info.CurrentRegion != null)
                {
                    var region = PzRegions.GetRegionById(info.CurrentRegion.ID);

                    if (region != null)
                    {
                        if (!info.BypassChange)
                        {
                            info.FakeSscStatus = true;

                            if (!info.InSscRegion)
                            {
                                info.InSscRegion = true;
                                info.SetBackupPlayerData();
                            }

                            if (region.HasDefault)
                                info.ChangeCharacter(region.GetDefaultData());

                            shouldInvokeLeave = false;
                        }
                    }
                }

                // 如果从区域出去，且没有进入新pz区域，则恢复
                if (shouldInvokeLeave && oldRegion != null)
                {
                    if (!info.InSscRegion || info.FakeSscStatus == DefaultFakeSscStatus)
                        continue;

                    info.RestoreCharacter();

                    info.InSscRegion = false;
                    info.FakeSscStatus = false;
                }
            }

            _lastCheck = DateTime.UtcNow;
        }

        private void OnOtapiSendData(SendDataEventArgs args)
        {
            if (args.MsgId != PacketTypes.WorldInfo)
            {
                return;
            }
            var remoteClient = args.remoteClient;
            if (remoteClient == -1)
            {
                var onData = PackInfo(true);
                var offData = PackInfo(false);

                foreach (var tsPlayer in TShock.Players.Where(p => p?.Active ?? false))
                {
                    if (!SendDataHooks.InvokePreSendData(remoteClient, tsPlayer.Index))
                        continue;
                    try
                    {
                        tsPlayer.SendRawData(PlayerInfo.GetPlayerInfo(tsPlayer).FakeSscStatus ?? DefaultFakeSscStatus ? onData : offData);
                        args.Handled = true;
                    }
                    catch
                    {
                        // ignored
                    }
                    SendDataHooks.InvokePostSendData(remoteClient, tsPlayer.Index);
                }
            }
            else
            {
                var player = TShock.Players.ElementAtOrDefault(remoteClient);

                if (player != null)
                {
                    var info = PlayerInfo.GetPlayerInfo(player);

                    /* 如果在区域内，收到了来自别的插件的发送请求
					 * 保持默认 ssc = true 并发送(也就是不需要改什么)
					 * 如果在区域外，收到了来自别的插件的发送请求
					 * 需要 fake ssc = false 并发送
					 */
                    SendInfo(remoteClient, info.FakeSscStatus ?? DefaultFakeSscStatus);
                    args.Handled = true;
                }
            }
        }

        private void OnPostInit(EventArgs args)
        {
            PzRegions.ReloadRegions();
        }

        private void OnInit(EventArgs args)
        {
            if (!TShock.ServerSideCharacterConfig.Settings.Enabled)
            {
                TShock.Log.ConsoleError("[Pz] 未开启SSC! 你可能选错了插件.");
                //Dispose(true);
                //throw new NotSupportedException("该插件不支持非SSC模式运行!");
            }

            Commands.ChatCommands.Add(new Command("pz.admin.manage", PzCmd, "pz") { AllowServer = false });
            Commands.ChatCommands.Add(new Command("pz.admin.toggle", ToggleBypass, "pztoggle") { AllowServer = false });
            Commands.ChatCommands.Add(new Command("pz.select", PzSelect, "pzselect") { AllowServer = false });

            PzRegions = new PzRegionManager(TShock.DB);
        }

        private void OnRegionDeleted(RegionHooks.RegionDeletedEventArgs args)
        {
            if (!PzRegions.PzRegions.Exists(p => p.Id == args.Region.ID))
                return;

            PzRegions.RemoveRegion(args.Region.ID);
        }

        private static void ToggleBypass(CommandArgs args)
        {
            var info = PlayerInfo.GetPlayerInfo(args.Player);

            info.BypassChange = !info.BypassChange;

            args.Player.SendSuccessMessage("{0}调整跳过装备更换模式。", info.BypassChange ? "关闭" : "开启");
        }

        private void PzSelect(CommandArgs args)
        {
            if (args.Parameters.Count == 0)
            {
                args.Player.SendErrorMessage("参数错误！正确用法：/pzselect <存档名>");
            }

            if (args.Player.CurrentRegion == null)
            {
                args.Player.SendInfoMessage("区域无效。");
                return;
            }

            var region = PzRegions.GetRegionById(args.Player.CurrentRegion.ID);
            if (region == null)
            {
                args.Player.SendInfoMessage("区域无效。");
                return;
            }

            var name = string.Join(" ", args.Parameters);

            if (!region.PlayerDatas.TryGetValue(name, out PlayerData data))
            {
                args.Player.SendInfoMessage("未找到对应存档名。");
                return;
            }

            PlayerInfo.GetPlayerInfo(args.Player).ChangeCharacter(data);
            args.Player.SendInfoMessage("你的人物存档被切换为{0}。", name);
        }

        private void PzCmd(CommandArgs args)
        {
            var cmd = args.Parameters.Count == 0 ? "HELP" : args.Parameters[0].ToUpperInvariant();

            switch (cmd)
            {
                case "ADD":
                    #region add
                    if (args.Parameters.Count < 3)
                    {
                        args.Player.SendErrorMessage("语法无效! 正确语法: /pz add <区域名> <存档名> [玩家名]");
                        return;
                    }

                    var regionName = args.Parameters[1];
                    var name = args.Parameters[2];
                    var playerName = args.Parameters.ElementAtOrDefault(3);

                    if (name.Length > 10)
                    {
                        args.Player.SendErrorMessage("存档名的长度不能超过10!");
                        return;
                    }

                    var region = TShock.Regions.GetRegionByName(regionName);
                    if (region == null)
                    {
                        args.Player.SendErrorMessage("区域名无效!");
                        return;
                    }
                    TSPlayer player = null;
                    if (!string.IsNullOrWhiteSpace(playerName))
                    {
                        var players = TSPlayer.FindByNameOrID(playerName);
                        if (players.Count == 0)
                        {
                            args.Player.SendErrorMessage("未找到玩家!");
                            return;
                        }
                        if (players.Count > 1)
                        {
                            args.Player.SendMultipleMatchError(players.Select(p => p.Name));
                            return;
                        }
                        player = players[0];
                    }
                    player = player ?? args.Player;
                    var data = new PlayerData(null);
                    data.CopyCharacter(player);

                    PzRegions.AddRegion(region.ID);
                    PzRegions.AddCharacter(region.ID, name, data);
                    args.Player.SendSuccessMessage("添加区域完毕.");
                    #endregion
                    break;
                case "LIST":
                    #region list
                    int pageNumber;
                    if (!PaginationTools.TryParsePageNumber(args.Parameters, 1, args.Player, out pageNumber))
                        return;
                    var names = from pz in PzRegions.PzRegions
                                select TShock.Regions.GetRegionByID(pz.Id).Name + ": " + string.Join(", ", pz.PlayerDatas.Keys);
                    PaginationTools.SendPage(args.Player, pageNumber, PaginationTools.BuildLinesFromTerms(names),
                        new PaginationTools.Settings
                        {
                            HeaderFormat = "应用区域 ({0}/{1}):",
                            FooterFormat = "键入 {0}pz list {{0}} 以获取下一页应用区域.".SFormat(Commands.Specifier),
                            NothingToDisplayString = "当前没有可用应用区域."
                        });
                    #endregion
                    break;
                case "REMOVE":
                    #region remove
                    if (args.Parameters.Count == 1)
                    {
                        args.Player.SendErrorMessage("语法无效! 正确语法: /pz remove <区域名>");
                        return;
                    }
                    regionName = string.Join(" ", args.Parameters.Skip(1));
                    region = TShock.Regions.GetRegionByName(regionName);
                    if (region == null)
                    {
                        args.Player.SendErrorMessage("区域名无效!");
                        return;
                    }

                    PzRegions.RemoveRegion(region.ID);
                    args.Player.SendSuccessMessage("删除区域及存档完毕.");
                    #endregion
                    break;
                case "REMOVECHAR":
                    #region removeChar
                    if (args.Parameters.Count < 3)
                    {
                        args.Player.SendErrorMessage("语法无效! 正确语法: /pz removechar <区域名> <存档名>");
                        return;
                    }
                    regionName = args.Parameters[1];
                    name = args.Parameters[2];
                    region = TShock.Regions.GetRegionByName(regionName);
                    if (region == null)
                    {
                        args.Player.SendErrorMessage("区域名无效!");
                        return;
                    }

                    PzRegions.RemoveCharacter(region.ID, name);
                    args.Player.SendSuccessMessage("删除存档完毕.");
                    #endregion
                    break;
                case "DEFAULT":
                    #region default
                    if (args.Parameters.Count < 3)
                    {
                        args.Player.SendErrorMessage("语法无效! 正确语法: /pz default <区域名> <存档名>");
                        return;
                    }
                    regionName = args.Parameters[1];
                    name = args.Parameters[2];
                    region = TShock.Regions.GetRegionByName(regionName);
                    if (region == null)
                    {
                        args.Player.SendErrorMessage("区域名无效!");
                        return;
                    }

                    var pzregion = PzRegions.GetRegionById(region.ID);
                    if (pzregion == null)
                    {
                        args.Player.SendErrorMessage("该区域并卟是Pz区域!");
                        return;
                    }
                    if (!pzregion.PlayerDatas.ContainsKey(name))
                    {
                        args.Player.SendErrorMessage("区域内未找到符合条件的存档!");
                        return;
                    }

                    PzRegions.SetDefaultCharacter(region.ID, name);
                    args.Player.SendSuccessMessage("设定存档完毕.");
                    #endregion
                    break;
                case "DELDEFAULT":
                    #region deldefault
                    if (args.Parameters.Count == 1)
                    {
                        args.Player.SendErrorMessage("语法无效! 正确语法: /pz deldefault <区域名>");
                        return;
                    }
                    regionName = string.Join(" ", args.Parameters.Skip(1));
                    region = TShock.Regions.GetRegionByName(regionName);
                    if (region == null)
                    {
                        args.Player.SendErrorMessage("区域名无效!");
                        return;
                    }

                    pzregion = PzRegions.GetRegionById(region.ID);
                    if (pzregion == null)
                    {
                        args.Player.SendErrorMessage("该区域并卟是Pz区域!");
                        return;
                    }

                    PzRegions.SetDefaultCharacter(region.ID, null);
                    args.Player.SendSuccessMessage("移除默认存档完毕.");
                    #endregion
                    break;
                case "SHOW":
                case "RESTORE":
                    args.Player.SendErrorMessage("暂不支持该功能.");
                    break;
                case "HELP":
                    #region help
                    if (!PaginationTools.TryParsePageNumber(args.Parameters, 1, args.Player, out pageNumber))
                        return;
                    var help = new[]
                    {
                        "add <区域名> <存档名> [玩家名(默认为自己)] - - 增加区域内存档",
                        "remove <区域名> - - 删除区域内所有存档",
                        "removechar <区域名> <存档名> - - 删除区域内存档",
                        "default <区域名> <存档名> - - 设置单一存档默认值",
                        "deldefault <区域名> - - 删除单一存档默认值",
                        "list [页码] - - 显示所有区域",
                        "help [页码] - - 显示子指令帮助"
                    };
                    PaginationTools.SendPage(args.Player, pageNumber, help,
                        new PaginationTools.Settings
                        {
                            HeaderFormat = "应用区域指令帮助 ({0}/{1}):",
                            FooterFormat = "键入 {0}pz help {{0}} 以获取下一页应用区域帮助.".SFormat(Commands.Specifier),
                            NothingToDisplayString = "当前没有可用帮助."
                        });
                    #endregion
                    break;
                default:
                    args.Player.SendErrorMessage("语法无效! 键入 /pz help 以获取帮助.");
                    return;
            }
        }

        private static void OnSendWorldInfo(PacketEventArgs args)
        {
            if (PlayerInfo.GetPlayerInfo(args.Player.TsPlayer) is { } plr)
            {
                var bb = (args.Packet as WorldData).EventInfo1;
                bb[6] = plr.FakeSscStatus ?? DefaultFakeSscStatus;

            }

        }
        private static byte[] PackInfo(bool ssc)
        {
            return BUtils.GetCurrentWorldData(ssc).SerializePacket();
        }
        internal static void SendInfo(int remoteClient, bool ssc)
        {
            if (!SendDataHooks.InvokePreSendData(remoteClient, remoteClient))
                return;

            TShock.Players[remoteClient]?.SendRawData(PackInfo(ssc));

            SendDataHooks.InvokePostSendData(remoteClient, remoteClient);
        }
    }
}
