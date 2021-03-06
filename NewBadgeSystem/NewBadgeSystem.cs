using Terraria;
using TerrariaApi.Server;
using TShockAPI;
using TShockAPI.Hooks;

namespace BadgeSystem
{
    [ApiVersion(2, 1)]
    public sealed class BadgeSystem : TerrariaPlugin
    {
        internal static BadgeManager Badges;

        internal static ContentConfig ContentConfig;

        public override string Name => GetType().Namespace;

        public override string Author => "Jeoican";

        public override Version Version => GetType().Assembly.GetName().Version;

        public BadgeSystem(Main game)
            : base(game)
        {
        }

        public override void Initialize()
        {
            PlayerHooks.PlayerChat += OnChat;
            ServerApi.Hooks.NetGreetPlayer.Register(this, OnGreet);
            ServerApi.Hooks.GameInitialize.Register(this, OnInitialize);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                PlayerHooks.PlayerChat -= OnChat;
                ServerApi.Hooks.NetGreetPlayer.Deregister(this, OnGreet);
                ServerApi.Hooks.GameInitialize.Deregister(this, OnInitialize);
            }
            base.Dispose(disposing);
        }

        private static void OnInitialize(EventArgs args)
        {
            Badges = new BadgeManager(TShock.DB);
            ReloadConfig();
            GeneralHooks.ReloadEvent += delegate
            {
                ReloadConfig();
            };
            Commands.ChatCommands.Add(new Command("badgesys.manage.admin", BdCmd, "badge", "bd"));
            Commands.ChatCommands.Add(new Command("badgesys.manage.player", BpCmd, "badgeplayer", "bp"));
            Commands.ChatCommands.Add(new Command("badgesys.manage.player", BadgeInfo, "badgeinfo", "bi"));
            static void ReloadConfig()
            {
                ContentConfig = ContentConfig.Read();
                ContentConfig.Write();
            }
        }
        private static void BpCmd(CommandArgs args)
        {
            var cmd = args.Parameters.Count == 0 ? "help" : args.Parameters[0].ToLower();
            switch (cmd)
            {
                //??????
                case "rmcp":
                case "rmcprefix":
                    RemoveCurrent(args, "prefix");
                    break;
                case "adcp":
                case "adcprefix":
                    AddCurrent(args, "prefix");
                    break;
                case "rmcs":
                case "rmcsuffix":
                    RemoveCurrent(args, "suffix");
                    break;
                case "adcs":
                case "adcsuffix":
                    AddCurrent(args, "suffix");
                    break;
                case "rmcb":
                case "rmcbrackets":
                    RemoveCurrent(args, "brackets");
                    break;
                case "adcb":
                case "adcbrackets":
                    AddCurrent(args, "brackets");
                    break;
                case "help":
                    #region help
                    int pageNumber;
                    if (!PaginationTools.TryParsePageNumber(args.Parameters, 1, args.Player, out pageNumber))
                        return;
                    var help = new[]
                    {
                        "adcprefix <?????????> - - ?????????????????????",
                        "rmcprefix [??????] <?????????> - - ??????????????????",
                        "adcsuffix <?????????> - - ?????????????????????",
                        "rmcsuffix [??????] <?????????> - - ??????????????????",
                        "adcbrackets <?????????> - - ?????????????????????",
                        "rmcbrackets [??????] <?????????> - - ??????????????????",
                    };
                    PaginationTools.SendPage(args.Player, pageNumber, help,
                        new PaginationTools.Settings
                        {
                            HeaderFormat = "BadgePlayer???????????? ({0}/{1}):",
                            FooterFormat = "?????? {0}bp help {{0}} ????????????????????????????????????.".SFormat(Commands.Specifier),
                            NothingToDisplayString = "????????????????????????."
                        });
                    #endregion
                    break;
                default:
                    args.Player.SendErrorMessage("????????????! ?????? /bp help ???????????????.");
                    return;
            }
        }
        private static void BdCmd(CommandArgs args)
        {
            var cmd = args.Parameters.Count == 0 ? "help" : args.Parameters[0].ToLower();
            switch (cmd)
            {
                //??????
                case "np":
                case "newprefix":
                    New(args, "prefix");
                    break;
                case "ap":
                case "addprefix":
                    Add(args, "prefix");
                    break;
                case "rmp":
                case "rmprefix":
                    Remove(args, "prefix");
                    break;
                case "dp":
                case "delPrefix":
                    Del(args, "prefix");
                    break;
                //??????
                case "ns":
                case "newsuffix":
                    New(args, "suffix");
                    break;
                case "as":
                case "addsuffix":
                    Add(args, "suffix");
                    break;
                case "rms":
                case "rmsuffix":
                    Remove(args, "suffix");
                    break;
                case "ds":
                case "delsuffix":
                    Del(args, "suffix");
                    break;
                //??????
                case "nb":
                case "newbrackets":
                    New(args, "brackets");
                    break;
                case "ab":
                case "addbrackets":
                    Add(args, "brackets");
                    break;
                case "rmb":
                case "rmbrackets":
                    Remove(args, "brackets");
                    break;
                case "db":
                case "delbrackets":
                    Del(args, "brackets");
                    break;
                case "help":
                    #region help
                    int pageNumber;
                    if (!PaginationTools.TryParsePageNumber(args.Parameters, 1, args.Player, out pageNumber))
                        return;
                    var help = new[]
                    {
                        "addprefix [??????] <?????????> - - ?????????????????????",
                        "rmprefix [??????] <?????????> - - ??????????????????",
                        "newprefix <??????> <?????????> <??????> - - ???????????????",
                        "delPrefix <?????????> - - ????????????",
                        "addsuffix [??????] <?????????> - - ?????????????????????",
                        "rmpsuffix [??????] <?????????> - - ??????????????????",
                        "newsuffix <??????> <?????????> <??????> - - ???????????????",
                        "delsuffix <?????????> - - ????????????",
                        "addbrackets [??????] <?????????> - - ?????????????????????",
                        "rmpbrackets [??????] <?????????> - - ??????????????????",
                        "newbrackets <??????> <?????????> <??????> - - ???????????????",
                        "delbrackets <?????????> - - ????????????"
                    };
                    PaginationTools.SendPage(args.Player, pageNumber, help,
                        new PaginationTools.Settings
                        {
                            HeaderFormat = "BadgeSystem???????????? ({0}/{1}):",
                            FooterFormat = "?????? {0}bd help {{0}} ????????????????????????????????????.".SFormat(Commands.Specifier),
                            NothingToDisplayString = "????????????????????????."
                        });
                    #endregion
                    break;
                default:
                    args.Player.SendErrorMessage("????????????! ?????? /bd help ???????????????.");
                    return;
            }
        }
        private static void OnGreet(GreetPlayerEventArgs args)
        {
            PlayerData.GetPlayerData(TShock.Players[args.Who]);
        }
        private static void OnChat(PlayerChatEventArgs args)
        {
            if (args.Handled)
                return;
            args.Handled = true;
            TSPlayer tSPlayer = args.Player;
            var playerData = PlayerData.GetPlayerData(tSPlayer);
            TShock.Utils.Broadcast(string.Format(TShock.Config.Settings.ChatFormat, tSPlayer.Group.Name, tSPlayer.Group.Prefix + playerData.Prefix, tSPlayer.Name, tSPlayer.Group.Suffix, args.RawText), new(tSPlayer.Group.R, tSPlayer.Group.G, tSPlayer.Group.B));
        }

        private static void BadgeInfo(CommandArgs args)
        {
            TSPlayer tSPlayer = args.Player;
            if (args.Parameters.Count > 0)
            {
                string search = string.Join(" ", args.Parameters);
                List<TSPlayer> list = TSPlayer.FindByNameOrID(search);
                if (list.Count == 0)
                {
                    args.Player.SendErrorMessage("??????????????????");
                    return;
                }
                if (list.Count > 1)
                {
                    args.Player.SendMultipleMatchError(list.Select((TSPlayer x) => x.Name));
                    return;
                }
                tSPlayer = list.Single();
            }
            if (!tSPlayer.RealPlayer)
            {
                args.Player.SendErrorMessage("?????????????????????");
                return;
            }
            PlayerData playerData = PlayerData.GetPlayerData(tSPlayer);
            args.Player.SendInfoMessage("???????????????" + string.Join(" ", playerData.TotalBrackets.Select((Content x) => "[c/" + x.ColorHex + ":" + x.ContentValue + "]")));
            args.Player.SendInfoMessage("???????????????" + string.Join(" ", playerData.TotalPrefix.Select((Content x) => "[c/" + x.ColorHex + ":" + x.ContentValue + "]")));
            args.Player.SendInfoMessage("???????????????" + string.Join(" ", playerData.TotalSuffix.Select((Content x) => "[c/" + x.ColorHex + ":" + x.ContentValue + "]")));
            args.Player.SendInfoMessage("???????????????" + playerData.Prefix);
        }
        private static void Add(CommandArgs args, string type)
        {    //      1      2           3
             // bd ap  [??????] <?????????>
            string result = BadgeManager.type2Chinese(type);
            if (args.Parameters.Count < 2)
            {
                args.Player.SendErrorMessage("????????????????????????add" + type + " [??????] <?????????>");
            }
            else if (args.Parameters.Count > 2)//3
            {
                List<TSPlayer> list = TSPlayer.FindByNameOrID(args.Parameters[1]);
                if (list.Count == 0)
                {
                    args.Player.SendErrorMessage("??????????????????");
                    return;
                }
                if (list.Count > 1)
                {
                    args.Player.SendMultipleMatchError(list.Select((TSPlayer x) => x.Name));
                    return;
                }
                TSPlayer player = list.Single();
                //string str = string.Join(" ", args.Parameters.Skip(1));
                string str = args.Parameters[2];
                if (!ContentConfig.TryParse(str, out Content b, type))
                {
                    args.Player.SendErrorMessage("??????????????????");
                    return;
                }
                PlayerData playerData = PlayerData.GetPlayerData(player);
                playerData.AddContent(b, type);
                args.Player.SendSuccessMessage("????????????: " + result);
            }
            else if (!args.Player.RealPlayer)
            {
                args.Player.SendErrorMessage("?????????????????????");
            }
            else//2 bd ap <?????????>
            {
                string str2 = args.Parameters[1];
                if (!ContentConfig.TryParse(str2, out Content b2, type))
                {
                    args.Player.SendErrorMessage("??????????????????");
                    return;
                }
                PlayerData playerData2 = PlayerData.GetPlayerData(args.Player);
                playerData2.AddContent(b2, type);
                args.Player.SendSuccessMessage("????????????: " + result);
            }
        }
        private static void New(CommandArgs args, string type)
        {    //     1       2          3           4
             //bd np  <??????> <?????????> [??????]
            string result = BadgeManager.type2Chinese(type);
            if (args.Parameters.Count == 0)
            {
                args.Player.SendErrorMessage("???????????????????????? new" + type + " <??????> <?????????> " + type == "brackets" ? "<??????>" : "" + "" + " ?????????bd new" + type + " Bomb bomb ff6a6a");
            }
            else if (args.Parameters.Count > 2)//3
            {
                string content = args.Parameters[1];
                string id = args.Parameters[2];
                string text = "";
                if (type == "brackets")//4
                {
                    if (args.Parameters.Count() != 4)
                    {
                        args.Player.SendErrorMessage("????????????????????????????????? ???newbrackets <??????> <?????????> <??????>");
                    }
                    text = args.Parameters[3];
                    if (!Content.TryParse(text, out var _))
                    {
                        args.Player.SendErrorMessage("??????????????????");
                        return;
                    }
                    if (!content.Contains(","))
                    {
                        args.Player.SendErrorMessage("???????????????????????????????????? ,  ??? (,)");
                        return;
                    }
                }
                Content b = new Content(content, id, text, type);
                TSPlayer[] players = TShock.Players;
                foreach (TSPlayer tSPlayer in players)
                {
                    if (tSPlayer != null)
                    {
                        PlayerData playerData = PlayerData.GetPlayerData(tSPlayer);
                        if (playerData.TotalPrefix.Any((Content i) => i.Identifier == b.Identifier))
                        {
                            playerData.AddContent(b, type);
                        }
                    }
                }
                ContentConfig.Content.RemoveAll((Content i) => i.Identifier == b.Identifier);
                ContentConfig.Content.Add(b);
                ContentConfig.Write();
                args.Player.SendSuccessMessage("????????????: " + result);
            }
            else
            {
                args.Player.SendErrorMessage("???????????????????????? new" + type + " <??????> <?????????> " + type == "brackets" ? "<??????>" : "" + "" + " ?????????bd new" + type + " Bomb bomb ff6a6a");
            }
        }
        private static void Del(CommandArgs args, string type)
        {    //      1      2
             //bd dp <?????????>
            string result = BadgeManager.type2Chinese(type);
            if (args.Parameters.Count == 0)
            {
                args.Player.SendErrorMessage("????????????????????????/del" + type + " <?????????>");
            }
            else if (args.Parameters.Count == 2)
            {
                string str = args.Parameters[1];
                if (!ContentConfig.TryParse(str, out Content b, type))
                {
                    args.Player.SendErrorMessage("?????????????????????????????????");
                    return;
                }
                TSPlayer[] players = TShock.Players;
                //TSPlayer[] array = players;
                foreach (TSPlayer tSPlayer in players)
                {
                    if (tSPlayer != null)
                    {
                        PlayerData.GetPlayerData(tSPlayer).RemoveContent(b, type);
                    }
                }
                ContentConfig.Content.Remove(b);
                ContentConfig.Write();
                args.Player.SendSuccessMessage("????????????: " + result);
            }
            else
            {
                args.Player.SendErrorMessage("????????????????????????/del" + type + " <?????????>");
            }
        }
        private static void Remove(CommandArgs args, string type)
        {    //     1      2          3
             //ab dp [??????] <?????????> 
            string result = BadgeManager.type2Chinese(type);
            if (args.Parameters.Count == 0)
            {
                args.Player.SendErrorMessage("????????????????????????/rm" + type + " [??????] <?????????>");
            }
            else if (args.Parameters.Count > 2)//3
            {
                List<TSPlayer> list = TSPlayer.FindByNameOrID(args.Parameters[1]);
                if (list.Count == 0)
                {
                    args.Player.SendErrorMessage("??????????????????");
                    return;
                }
                if (list.Count > 1)
                {
                    args.Player.SendMultipleMatchError(list.Select((TSPlayer x) => x.Name));
                    return;
                }
                TSPlayer player = list.Single();
                string str = args.Parameters[2];
                if (!ContentConfig.TryParse(str, out Content b, type))
                {
                    args.Player.SendErrorMessage("??????????????????");
                    return;
                }
                PlayerData playerData = PlayerData.GetPlayerData(player);
                playerData.RemoveContent(b, type);
                args.Player.SendSuccessMessage("????????????: " + result);
            }
            else if (!args.Player.RealPlayer)
            {
                args.Player.SendErrorMessage("?????????????????????");
            }
            else if (args.Parameters.Count == 2)//2
            {
                string str2 = args.Parameters[1];
                if (!ContentConfig.TryParse(str2, out Content b2, type))
                {
                    args.Player.SendErrorMessage("??????????????????");
                    return;
                }
                PlayerData playerData2 = PlayerData.GetPlayerData(args.Player);
                playerData2.RemoveContent(b2, type);
                args.Player.SendSuccessMessage("????????????: " + result);
            }
            else
            {
                args.Player.SendErrorMessage("????????????????????????/rm" + type + " [??????] <?????????>");
            }
        }
        private static void RemoveCurrent(CommandArgs args, string type)
        {   //        1        2           3
            //bp rmcb  [??????] <?????????>
            string result = BadgeManager.type2Chinese(type);
            if (args.Parameters.Count < 2)
            {
                args.Player.SendErrorMessage("????????????????????????/rmc" + type + " [??????] <?????????>");
            }
            else if (args.Parameters.Count == 3)//3
            {
                List<TSPlayer> list = TSPlayer.FindByNameOrID(args.Parameters[1]);
                if (list.Count == 0)
                {
                    args.Player.SendErrorMessage("??????????????????");
                    return;
                }
                if (list.Count > 1)
                {
                    args.Player.SendMultipleMatchError(list.Select((TSPlayer x) => x.Name));
                    return;
                }
                TSPlayer player = list.Single();
                string str = args.Parameters[2];
                if (!ContentConfig.TryParse(str, out Content b, type))
                {
                    args.Player.SendErrorMessage("??????????????????");
                    return;
                }
                PlayerData playerData = PlayerData.GetPlayerData(player);
                playerData.RemoveCurrentContent(b, type);
                args.Player.SendSuccessMessage("????????????: " + result);
            }
            else if (!args.Player.RealPlayer)
            {
                args.Player.SendErrorMessage("?????????????????????");
            }
            else if (args.Parameters.Count == 2)
            {
                string str2 = args.Parameters[1];
                if (!ContentConfig.TryParse(str2, out Content b2, type))
                {
                    args.Player.SendErrorMessage("??????????????????");
                    return;
                }
                PlayerData playerData2 = PlayerData.GetPlayerData(args.Player);
                playerData2.RemoveCurrentContent(b2, type);
                args.Player.SendSuccessMessage("????????????: " + result);
            }
            else
            {
                args.Player.SendErrorMessage("????????????????????????/rmc" + type + " [??????] <?????????>");
            }
        }
        private static void AddCurrent(CommandArgs args, string type)
        {   //        1          2
            //bp adcp  <?????????>
            string result = BadgeManager.type2Chinese(type);
            if (!args.Player.RealPlayer)
            {
                args.Player.SendErrorMessage("?????????????????????");
                return;
            }
            if (args.Parameters.Count < 2)
            {
                args.Player.SendErrorMessage("????????????????????????adc" + type + " <?????????>");
                return;
            }
            else if (args.Parameters.Count == 2)
            {
                string str = args.Parameters[1];
                if (!ContentConfig.TryParse(str, out Content b, type))
                {
                    args.Player.SendErrorMessage("??????????????????");
                    return;
                }
                PlayerData playerData = PlayerData.GetPlayerData(args.Player);
                bool flag = false;
                switch (type)
                {
                    case "prefix": flag = playerData.TotalPrefix.Contains(b); break;
                    case "suffix": flag = playerData.TotalSuffix.Contains(b); break;
                    case "brackets": flag = playerData.TotalBrackets.Contains(b); break;
                }
                if (!flag)
                {
                    args.Player.SendErrorMessage("????????????" + result);
                    return;
                }
                playerData.RemoveCurrentAll(type);
                playerData.AddCurrentContent(b, type);
                args.Player.SendSuccessMessage("????????????: " + result);
            }
            else
            {
                args.Player.SendErrorMessage("????????????????????????adc" + type + " <?????????>");
            }
        }
    }
}
