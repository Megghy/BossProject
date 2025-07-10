#region Using
using TShockAPI;
#endregion

namespace FakeProvider.Handlers
{
    /// <summary>
    /// 处理假Provider的命令相关逻辑
    /// </summary>
    internal static class CommandHandler
    {
        private static readonly object _providerLock = new object();

        #region Provider查找

        /// <summary>
        /// 根据名称查找Provider
        /// </summary>
        /// <param name="name">Provider名称</param>
        /// <param name="player">执行命令的玩家</param>
        /// <param name="provider">找到的Provider</param>
        /// <param name="includeGlobal">是否包含全局Provider</param>
        /// <param name="includePersonal">是否包含个人Provider</param>
        /// <returns>是否找到唯一的Provider</returns>
        public static bool FindProvider(string name, TSPlayer player, out TileProvider provider, bool includeGlobal = true, bool includePersonal = false)
        {
            provider = null;
            IEnumerable<TileProvider> foundProviders;
            lock (_providerLock)
            {
                // 在锁内立即执行查询，使其在外部安全使用
                foundProviders = FakeProviderAPI.FindProvider(name, includeGlobal, includePersonal).ToList();
            }

            if (foundProviders.Count() == 0)
            {
                player?.SendErrorMessage("Invalid provider '" + name + "'");
                return false;
            }
            if (foundProviders.Count() > 1)
            {
                player?.SendMultipleMatchError(foundProviders);
                return false;
            }
            provider = foundProviders.First();
            return true;
        }

        #endregion

        #region 命令处理

        /// <summary>
        /// 处理全局 fake 命令
        /// </summary>
        /// <param name="args">命令参数</param>
        public static void FakeCommand(CommandArgs args) => HandleFakeCommand(args, false);

        /// <summary>
        /// 处理个人 pfake 命令
        /// </summary>
        /// <param name="args">命令参数</param>
        public static void PersonalFakeCommand(CommandArgs args) => HandleFakeCommand(args, true);

        /// <summary>
        /// 统一处理 fake 命令逻辑
        /// </summary>
        /// <param name="args">命令参数</param>
        /// <param name="isPersonal">是否为个人Provider命令</param>
        private static void HandleFakeCommand(CommandArgs args, bool isPersonal)
        {
            string commandPrefix = isPersonal ? "/pfake" : "/fake";
            string providerType = isPersonal ? "personal" : "global";

            string arg0 = args.Parameters.ElementAtOrDefault(0);
            switch (arg0?.ToLower())
            {
                case "l":
                case "list":
                    {
                        bool allPersonalProviders = isPersonal && args.Parameters.RemoveAll(s => s == "all") > 0;
                        if (!PaginationTools.TryParsePageNumber(args.Parameters, 1, args.Player, out int page))
                            return;

                        IEnumerable<TileProvider> providersToList;
                        lock (_providerLock)
                        {

                            if (isPersonal)
                            {
                                if (allPersonalProviders)
                                    providersToList = FakeProviderAPI.Tile.Personal.ToList();
                                else
                                    providersToList = FakeProviderAPI.Tile.Personal.Where(provider => provider.Observers.Contains(args.Player.Index)).ToList();
                            }
                            else
                            {
                                providersToList = FakeProviderAPI.Tile.Global.ToList();
                            }
                        }

                        PaginationTools.SendPage(args.Player, page,
                            PaginationTools.BuildLinesFromTerms(providersToList),
                            new PaginationTools.Settings()
                            {
                                HeaderFormat = $"{(isPersonal ? "Personal fake" : "Fake")} providers ({{0}}/{{1}}):",
                                FooterFormat = $"Type '{commandPrefix} list {{0}}' for more.",
                                NothingToDisplayString = $"There are no {providerType} fake providers yet."
                            });
                        break;
                    }
                case "tp":
                case "teleport":
                    {
                        if (args.Parameters.Count != 2)
                        {
                            args.Player.SendErrorMessage($"{commandPrefix} tp \"provider name\"");
                            return;
                        }
                        if (!FindProvider(args.Parameters[1], args.Player, out TileProvider provider, !isPersonal, isPersonal))
                            return;

                        args.Player.Teleport((provider.X + provider.Width / 2) * 16,
                            (provider.Y + provider.Height / 2) * 16);
                        args.Player.SendSuccessMessage($"Teleported to fake provider '{provider.Name}'.");
                        break;
                    }
                case "m":
                case "move":
                    {
                        if (args.Parameters.Count != 4)
                        {
                            args.Player.SendErrorMessage($"{commandPrefix} move \"provider name\" <relative x> <relative y>");
                            return;
                        }
                        if (!FindProvider(args.Parameters[1], args.Player, out TileProvider provider, !isPersonal, isPersonal))
                            return;

                        if (!Int32.TryParse(args.Parameters[2], out int x)
                            || !Int32.TryParse(args.Parameters[3], out int y))
                        {
                            args.Player.SendErrorMessage("Invalid coordinates.");
                            return;
                        }

                        provider.Move(x, y, true);
                        args.Player.SendSuccessMessage($"Fake provider '{provider.Name}' moved to ({x}, {y}).");
                        break;
                    }
                case "la":
                case "layer":
                    {
                        if (args.Parameters.Count != 3)
                        {
                            args.Player.SendErrorMessage($"{commandPrefix} layer \"provider name\" <layer>");
                            return;
                        }
                        if (!FindProvider(args.Parameters[1], args.Player, out TileProvider provider, !isPersonal, isPersonal))
                            return;

                        if (!Int32.TryParse(args.Parameters[2], out int layer))
                        {
                            args.Player.SendErrorMessage("Invalid layer.");
                            return;
                        }

                        provider.SetLayer(layer, true);
                        args.Player.SendSuccessMessage($"Fake provider '{provider.Name}' layer set to {layer}.");
                        break;
                    }
                case "i":
                case "info":
                    {
                        if (args.Parameters.Count != 2)
                        {
                            args.Player.SendErrorMessage($"{commandPrefix} info \"provider name\"");
                            return;
                        }
                        if (!FindProvider(args.Parameters[1], args.Player, out TileProvider provider, !isPersonal, isPersonal))
                            return;

                        args.Player.SendInfoMessage(
            $@"Fake provider '{provider.Name}' ({provider.GetType().Name})
Position and size: {provider.XYWH()}
Enabled: {provider.Enabled}
Entities: {provider._entityManager.Entities.Count}");
                        break;
                    }
                case "d":
                case "disable":
                    {
                        if (args.Parameters.Count != 2)
                        {
                            args.Player.SendErrorMessage($"{commandPrefix} disable \"provider name\"");
                            return;
                        }
                        if (!FindProvider(args.Parameters[1], args.Player, out TileProvider provider, !isPersonal, isPersonal))
                            return;

                        provider.Disable();
                        break;
                    }
                case "e":
                case "enable":
                    {
                        if (args.Parameters.Count != 2)
                        {
                            args.Player.SendErrorMessage($"{commandPrefix} enable \"provider name\"");
                            return;
                        }
                        if (!FindProvider(args.Parameters[1], args.Player, out TileProvider provider, !isPersonal, isPersonal))
                            return;

                        provider.Enable();
                        break;
                    }
                default:
                    {
                        args.Player.SendSuccessMessage($"{commandPrefix} subcommands:");
                        args.Player.SendInfoMessage(
            $@"{commandPrefix} info ""provider name""
{commandPrefix} tp ""provider name""
{commandPrefix} move ""provider name"" <relative x> <relative y>
{commandPrefix} layer ""provider name"" <layer>
{commandPrefix} disable ""provider name""
{commandPrefix} enable ""provider name""
{commandPrefix} list [page]");
                        break;
                    }
            }
        }

        #endregion
    }
}