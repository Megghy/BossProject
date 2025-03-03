using Terraria;
using TerrariaApi.Server;
using TShockAPI;
using TShockAPI.Hooks;

namespace BadgeSystem
{
    [ApiVersion(2, 1)]
    public sealed class BadgeSystem : TerrariaPlugin
    {
        private static ContentConfig contentConfig;
        public static ContentConfig ContentConfig
        {
            get
            {
                contentConfig ??= ContentConfig.Read();
                return contentConfig;
            }
            set
            {
                contentConfig = value;
                contentConfig.Write();
            }
        }

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
            ServerApi.Hooks.GamePostInitialize.Register(this, OnInitialize);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                PlayerHooks.PlayerChat -= OnChat;
                ServerApi.Hooks.NetGreetPlayer.Deregister(this, OnGreet);
                ServerApi.Hooks.GamePostInitialize.Deregister(this, OnInitialize);
            }
            base.Dispose(disposing);
        }

        private static void OnInitialize(EventArgs args)
        {
            //Badges = new BadgeManager(TShock.DB);
            //ReloadConfig();
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
                //前缀
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
                        "adcprefix <识别号> - - 给玩家佩戴前缀",
                        "rmcprefix [玩家] <识别号> - - 摘下玩家前缀",
                        "adcsuffix <识别号> - - 给玩家佩戴后缀",
                        "rmcsuffix [玩家] <识别号> - - 摘下玩家后缀",
                        "adcbrackets <识别号> - - 给玩家佩戴括号",
                        "rmcbrackets [玩家] <识别号> - - 摘下玩家括号",
                    };
                    PaginationTools.SendPage(args.Player, pageNumber, help,
                        new PaginationTools.Settings
                        {
                            HeaderFormat = "BadgePlayer指令帮助 ({0}/{1}):",
                            FooterFormat = "键入 {0}bp help {{0}} 以获取下一页应用区域帮助.".SFormat(Commands.Specifier),
                            NothingToDisplayString = "当前没有可用帮助."
                        });
                    #endregion
                    break;
                default:
                    args.Player.SendErrorMessage("语法无效! 键入 /bp help 以获取帮助.");
                    return;
            }
        }
        private static void BdCmd(CommandArgs args)
        {
            var cmd = args.Parameters.Count == 0 ? "help" : args.Parameters[0].ToLower();
            switch (cmd)
            {
                //前缀
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
                //后缀
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
                //括号
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
                        "addprefix [玩家] <识别号> - - 给玩家添加前缀",
                        "rmprefix [玩家] <识别号> - - 移除玩家前缀",
                        "newprefix <内容> <识别号> <颜色> - - 创建新前缀",
                        "delPrefix <识别号> - - 删除前缀",
                        "addsuffix [玩家] <识别号> - - 给玩家添加后缀",
                        "rmpsuffix [玩家] <识别号> - - 移除玩家后缀",
                        "newsuffix <内容> <识别号> <颜色> - - 创建新后缀",
                        "delsuffix <识别号> - - 删除后缀",
                        "addbrackets [玩家] <识别号> - - 给玩家添加括号",
                        "rmpbrackets [玩家] <识别号> - - 移除玩家括号",
                        "newbrackets <内容> <识别号> <颜色> - - 创建新括号",
                        "delbrackets <识别号> - - 删除括号"
                    };
                    PaginationTools.SendPage(args.Player, pageNumber, help,
                        new PaginationTools.Settings
                        {
                            HeaderFormat = "BadgeSystem指令帮助 ({0}/{1}):",
                            FooterFormat = "键入 {0}bd help {{0}} 以获取下一页应用区域帮助.".SFormat(Commands.Specifier),
                            NothingToDisplayString = "当前没有可用帮助."
                        });
                    #endregion
                    break;
                default:
                    args.Player.SendErrorMessage("语法无效! 键入 /bd help 以获取帮助.");
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
                    args.Player.SendErrorMessage("未找到玩家。");
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
                args.Player.SendErrorMessage("缺少玩家参数。");
                return;
            }
            PlayerData playerData = PlayerData.GetPlayerData(tSPlayer);
            args.Player.SendInfoMessage("拥有括号：" + string.Join(" ", playerData.TotalBrackets.Select((Content x) => "[c/" + x.ColorHex + ":" + x.ContentValue + "]")));
            args.Player.SendInfoMessage("拥有前缀：" + string.Join(" ", playerData.TotalPrefix.Select((Content x) => "[c/" + x.ColorHex + ":" + x.ContentValue + "]")));
            args.Player.SendInfoMessage("拥有后缀：" + string.Join(" ", playerData.TotalSuffix.Select((Content x) => "[c/" + x.ColorHex + ":" + x.ContentValue + "]")));
            args.Player.SendInfoMessage("当前佩戴：" + playerData.Prefix);
        }
        private static void Add(CommandArgs args, string type)
        {    //      1      2           3
             // bd ap  [玩家] <识别号>
            string result = BadgeManager.type2Chinese(type);
            if (args.Parameters.Count < 2)
            {
                args.Player.SendErrorMessage("语法错误！用法：add" + type + " [玩家] <识别号>");
            }
            else if (args.Parameters.Count > 2)//3
            {
                List<TSPlayer> list = TSPlayer.FindByNameOrID(args.Parameters[1]);
                if (list.Count == 0)
                {
                    args.Player.SendErrorMessage("未找到玩家。");
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
                    args.Player.SendErrorMessage("识别号错误。");
                    return;
                }
                PlayerData playerData = PlayerData.GetPlayerData(player);
                playerData.AddContent(b, type);
                args.Player.SendSuccessMessage("完成添加: " + result);
            }
            else if (!args.Player.RealPlayer)
            {
                args.Player.SendErrorMessage("缺少玩家参数。");
            }
            else//2 bd ap <识别号>
            {
                string str2 = args.Parameters[1];
                if (!ContentConfig.TryParse(str2, out Content b2, type))
                {
                    args.Player.SendErrorMessage("识别号错误。");
                    return;
                }
                PlayerData playerData2 = PlayerData.GetPlayerData(args.Player);
                playerData2.AddContent(b2, type);
                args.Player.SendSuccessMessage("完成添加: " + result);
            }
        }
        private static void New(CommandArgs args, string type)
        {    //     1       2          3           4
             //bd np  <内容> <识别号> [颜色]
            string result = BadgeManager.type2Chinese(type);
            if (args.Parameters.Count == 0)
            {
                args.Player.SendErrorMessage("语法错误！用法： new" + type + " <内容> <识别号> " + type == "brackets" ? "<颜色>" : "" + "" + " 示例：bd new" + type + " Bomb bomb ff6a6a");
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
                        args.Player.SendErrorMessage("创建括号错误！正确语法 ：newbrackets <内容> <识别号> <颜色>");
                    }
                    text = args.Parameters[3];
                    if (!Content.TryParse(text, out var _))
                    {
                        args.Player.SendErrorMessage("颜色代码错误");
                        return;
                    }
                    if (!content.Contains(","))
                    {
                        args.Player.SendErrorMessage("格式错误！括号内容需含有 ,  如 (,)");
                        return;
                    }
                }
                AddBadge(id, content, text, type);
                args.Player.SendSuccessMessage("完成创建: " + result);
            }
            else
            {
                args.Player.SendErrorMessage("语法错误！用法： new" + type + " <内容> <识别号> " + type == "brackets" ? "<颜色>" : "" + "" + " 示例：bd new" + type + " Bomb bomb ff6a6a");
            }
        }
        public static Content AddBadge(string identifier, string content, string color, string type)
        {
            if (color.Length != 3 && color.Length != 6)
            {
                throw new ArgumentOutOfRangeException(nameof(color));
            }
            Content b = new Content(content, identifier, color, type);
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
            return b;
        }
        private static void Del(CommandArgs args, string type)
        {    //      1      2
             //bd dp <识别号>
            string result = BadgeManager.type2Chinese(type);
            if (args.Parameters.Count == 0)
            {
                args.Player.SendErrorMessage("语法错误！用法：/del" + type + " <识别号>");
            }
            else if (args.Parameters.Count == 2)
            {
                string str = args.Parameters[1];
                if (!ContentConfig.TryParse(str, out Content b, type))
                {
                    args.Player.SendErrorMessage("无对应前缀或识别号错误");
                    return;
                }
                DeleteBadge(b);
                args.Player.SendSuccessMessage("完成删除: " + result);
            }
            else
            {
                args.Player.SendErrorMessage("语法错误！用法：/del" + type + " <识别号>");
            }
        }
        public static void DeleteBadge(Content c)
        {
            TSPlayer[] players = TShock.Players;
            //TSPlayer[] array = players;
            foreach (TSPlayer tSPlayer in players)
            {
                if (tSPlayer != null)
                {
                    PlayerData.GetPlayerData(tSPlayer).RemoveContent(c, c.Type);
                }
            }
            ContentConfig.Content.Remove(c);
            ContentConfig.Write();
        }
        private static void Remove(CommandArgs args, string type)
        {    //     1      2          3
             //ab dp [玩家] <识别号> 
            string result = BadgeManager.type2Chinese(type);
            if (args.Parameters.Count == 0)
            {
                args.Player.SendErrorMessage("语法错误！用法：/rm" + type + " [玩家] <识别号>");
            }
            else if (args.Parameters.Count > 2)//3
            {
                List<TSPlayer> list = TSPlayer.FindByNameOrID(args.Parameters[1]);
                if (list.Count == 0)
                {
                    args.Player.SendErrorMessage("未找到玩家。");
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
                    args.Player.SendErrorMessage("识别号错误。");
                    return;
                }
                PlayerData playerData = PlayerData.GetPlayerData(player);
                playerData.RemoveContent(b, type);
                args.Player.SendSuccessMessage("完成删除: " + result);
            }
            else if (!args.Player.RealPlayer)
            {
                args.Player.SendErrorMessage("缺少玩家参数。");
            }
            else if (args.Parameters.Count == 2)//2
            {
                string str2 = args.Parameters[1];
                if (!ContentConfig.TryParse(str2, out Content b2, type))
                {
                    args.Player.SendErrorMessage("识别号错误。");
                    return;
                }
                PlayerData playerData2 = PlayerData.GetPlayerData(args.Player);
                playerData2.RemoveContent(b2, type);
                args.Player.SendSuccessMessage("完成删除: " + result);
            }
            else
            {
                args.Player.SendErrorMessage("语法错误！用法：/rm" + type + " [玩家] <识别号>");
            }
        }
        private static void RemoveCurrent(CommandArgs args, string type)
        {   //        1        2           3
            //bp rmcb  [玩家] <识别号>
            string result = BadgeManager.type2Chinese(type);
            if (args.Parameters.Count < 2)
            {
                args.Player.SendErrorMessage("语法错误！用法：/rmc" + type + " [玩家] <识别号>");
            }
            else if (args.Parameters.Count == 3)//3
            {
                List<TSPlayer> list = TSPlayer.FindByNameOrID(args.Parameters[1]);
                if (list.Count == 0)
                {
                    args.Player.SendErrorMessage("未找到玩家。");
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
                    args.Player.SendErrorMessage("识别号错误。");
                    return;
                }
                PlayerData playerData = PlayerData.GetPlayerData(player);
                playerData.RemoveCurrentContent(b, type);
                args.Player.SendSuccessMessage("完成摘下: " + result);
            }
            else if (!args.Player.RealPlayer)
            {
                args.Player.SendErrorMessage("缺少玩家参数。");
            }
            else if (args.Parameters.Count == 2)
            {
                string str2 = args.Parameters[1];
                if (!ContentConfig.TryParse(str2, out Content b2, type))
                {
                    args.Player.SendErrorMessage("识别号错误。");
                    return;
                }
                PlayerData playerData2 = PlayerData.GetPlayerData(args.Player);
                playerData2.RemoveCurrentContent(b2, type);
                args.Player.SendSuccessMessage("完成摘下: " + result);
            }
            else
            {
                args.Player.SendErrorMessage("语法错误！用法：/rmc" + type + " [玩家] <识别号>");
            }
        }
        private static void AddCurrent(CommandArgs args, string type)
        {   //        1          2
            //bp adcp  <识别号>
            string result = BadgeManager.type2Chinese(type);
            if (!args.Player.RealPlayer)
            {
                args.Player.SendErrorMessage("缺少玩家参数。");
                return;
            }
            if (args.Parameters.Count < 2)
            {
                args.Player.SendErrorMessage("语法错误！用法：adc" + type + " <识别号>");
                return;
            }
            else if (args.Parameters.Count == 2)
            {
                string str = args.Parameters[1];
                if (!ContentConfig.TryParse(str, out Content b, type))
                {
                    args.Player.SendErrorMessage("识别号错误。");
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
                    args.Player.SendErrorMessage("你没有该" + result);
                    return;
                }
                playerData.RemoveCurrentAll(type);
                playerData.AddCurrentContent(b, type);
                args.Player.SendSuccessMessage("完成佩戴: " + result);
            }
            else
            {
                args.Player.SendErrorMessage("语法错误！用法：adc" + type + " <识别号>");
            }
        }
    }
}
