#region Using
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using BossFramework;
using BossFramework.BCore;
using BossFramework.BModels;
using Terraria;
using Terraria.DataStructures;
using Terraria.GameContent.Events;
using Terraria.ID;
using Terraria.IO;
using Terraria.Net.Sockets;
using Terraria.Social;
using Terraria.Utilities;
using TerrariaApi.Server;
using TrProtocol;
using EnchCoreApi.TrProtocol.NetPackets;
using TShockAPI;
#endregion
namespace FakeProvider
{
    [ApiVersion(2, 1)]
    public class FakeProviderPlugin : TerrariaPlugin
    {
        [DllImport("User32.dll", CharSet = CharSet.Unicode)]
        public static extern int MessageBox(IntPtr h, string m, string c, int type);

        #region Data

        public override string Name => "FakeProvider";
        public override string Author => "ASgo and Anzhelika";
        public override string Description => "TODO";
        public override Version Version => Assembly.GetExecutingAssembly().GetName().Version;
        internal static int[] AllPlayers;
        private static long LoadWorldSize;
        public static string Debug;

        public static int OffsetX { get; private set; } // Not supported
        public static int OffsetY { get; private set; } // Not supported
        public static int VisibleWidth { get; private set; }
        public static int VisibleHeight { get; private set; }
        public static bool FastWorldLoad = true;

        internal static List<TileProvider> ProvidersToAdd = new();
        internal static bool ProvidersLoaded = false;
        public static Command[] CommandList = new Command[]
        {
            new Command("FakeProvider.Control", FakeCommand, "fake")
        };

        #endregion

        #region Constructor

        public FakeProviderPlugin(Main game) : base(game)
        {
            //Order = -1002;
            string[] args = Environment.GetCommandLineArgs();
            int argumentIndex;
            #region Offset

            /*int offsetX = 0;
            argumentIndex = Array.FindIndex(args, (x => (x.ToLower() == "-offsetx")));
            if (argumentIndex > -1)
            {
                argumentIndex++;
                if ((argumentIndex >= args.Length)
                        || !int.TryParse(args[argumentIndex], out offsetX))
                    Console.WriteLine("Please provide a not negative offsetX integer value.");
            }
            OffsetX = offsetX;

            int offsetY = 0;
            argumentIndex = Array.FindIndex(args, (x => (x.ToLower() == "-offsety")));
            if (argumentIndex > -1)
            {
                argumentIndex++;
                if ((argumentIndex >= args.Length)
                        || !int.TryParse(args[argumentIndex], out offsetY))
                    Console.WriteLine("Please provide a not negative offsetY integer value.");
            }
            OffsetY = offsetY;*/

            #endregion
            #region VisibleWidth, VisibleHeight

            int visibleWidth = -1;
            argumentIndex = Array.FindIndex(args, (x => (x.ToLower() == "-visiblewidth")));
            if (argumentIndex > -1)
            {
                argumentIndex++;
                if ((argumentIndex >= args.Length)
                        || !int.TryParse(args[argumentIndex], out visibleWidth))
                {
                    Console.WriteLine("Please provide a not negative visibleWidth integer value.");
                    visibleWidth = -1;
                }
            }
            VisibleWidth = visibleWidth;

            int visibleHeight = -1;
            argumentIndex = Array.FindIndex(args, (x => (x.ToLower() == "-visibleheight")));
            if (argumentIndex > -1)
            {
                argumentIndex++;
                if ((argumentIndex >= args.Length)
                        || !int.TryParse(args[argumentIndex], out visibleHeight))
                {
                    Console.WriteLine("Please provide a not negative visibleHeight integer value.");
                    visibleHeight = -1;
                }
            }
            VisibleHeight = visibleHeight;

            #endregion

            //TODO: rockLevel, surfaceLevel, cavernLevel or whatever

            // WARNING: has not been heavily tested
            //FastWorldLoad = args.Any(x => (x.ToLower() == "-fastworldload"));
        }

        #endregion
        #region Initialize

        public override void Initialize()
        {
            AllPlayers = new int[Main.maxPlayers];
            for (int i = 0; i < Main.maxPlayers; i++)
                AllPlayers[i] = i;

            //ServerApi.Hooks.world.Register(this, OnPreLoadWorld);

            HookEvents.Terraria.IO.WorldFile.LoadHeader += WorldFile_LoadHeader;
            HookEvents.Terraria.IO.WorldFile.LoadWorld += OnPreLoadWorld;
            ServerApi.Hooks.WorldSave.Register(this, OnPreSaveWorld);
            //ServerApi.Hooks.NetSendData.Register(this, OnSendData, Int32.MaxValue);
            ServerApi.Hooks.ServerLeave.Register(this, OnServerLeave);

            SignRedirector.SignRead += OnRequestSign;
            SignRedirector.SignUpdate += OnUpdateSign;
            ChestRedirector.ChestOpen += OnRequestChest;
            ChestRedirector.ChestSyncActive += OnCloseChest;
            ChestRedirector.ChestUpdateItem += OnUpdateChest;

            Commands.ChatCommands.AddRange(CommandList);
        }

        private void WorldFile_LoadHeader(object? sender, HookEvents.Terraria.IO.WorldFile.LoadHeaderEventArgs e)
        {
            e.ContinueExecution = false;
            e.OriginalMethod.Invoke(e.reader);

            Console.WriteLine("[FakeProvider] Loading custom tile provider.");
            CreateCustomTileProvider();
        }

        #endregion
        #region Dispose

        protected override void Dispose(bool Disposing)
        {
            if (Disposing)
            {
                //ServerApi.Hooks.WorldLoad.Deregister(this, OnPreLoadWorld);
                //ServerApi.Hooks.PostWorldLoad.Deregister(this, OnPostLoadWorld);
                HookEvents.Terraria.IO.WorldFile.LoadWorld -= OnPreLoadWorld;
                WorldGen.Hooks.OnWorldLoad -= OnPostLoadWorld;
                ServerApi.Hooks.WorldSave.Deregister(this, OnPreSaveWorld);
                ServerApi.Hooks.NetSendData.Deregister(this, OnSendData);
                ServerApi.Hooks.ServerLeave.Deregister(this, OnServerLeave);

                SignRedirector.SignRead -= OnRequestSign;
                SignRedirector.SignUpdate -= OnUpdateSign;
                ChestRedirector.ChestOpen -= OnRequestChest;
                ChestRedirector.ChestSyncActive -= OnCloseChest;
                ChestRedirector.ChestUpdateItem -= OnUpdateChest;
            }
            base.Dispose(Disposing);
        }

        #endregion

        #region 箱子牌子
        private static T GetTopEntity<T>(int x, int y, int targetPlrIndex = -1) where T : IFake
        {
            var providers = FakeProviderAPI.Tile.Providers.Where(p => p != null
            && p.Name != FakeProviderAPI.WorldProviderName
            && p.Enabled
            && (!(p.Observers?.Any() == true) || (p.Observers?.Contains(targetPlrIndex) ?? true)))
                .OrderBy(p => p.Order);
            foreach (var p in providers)
            {
                if (p.Entities.FirstOrDefault(e => e is T result && result.X == x && result.Y == y) is { } result)
                    return (T)result;
            }
            return default;
        }
        private static void OnUpdateSign(BEventArgs.SignUpdateEventArgs args)
        {
            if (GetTopEntity<FakeSign>(args.Position.X, args.Position.Y, args.Player.Index) is { })
                args.Handled = true;
        }
        private static void OnRequestSign(BEventArgs.SignReadEventArgs args)
        {
            if (GetTopEntity<FakeSign>(args.Position.X, args.Position.Y, args.Player.Index) is { } sign)
            {
                args.Handled = true;
                SignRedirector.SendSign(args.Player, (short)sign.x, (short)sign.y, sign.text);
            }
        }
        private static void OnRequestChest(BEventArgs.ChestOpenEventArgs args)
        {
            if (GetTopEntity<FakeChest>(args.Position.X, args.Position.Y, args.Player.Index) is { } chest)
            {
                args.Handled = true;

                List<Packet> list = new();
                40.For(i =>
                {
                    if (i < chest.item.Length)
                    {
                        chest.item[i] ??= new();
                        var c = chest.item[i];
                        list.Add(new SyncChestItem()
                        {
                            ChestSlot = 7998,
                            ChestItemSlot = (byte)i,
                            ItemType = (short)c.type,
                            Prefix = c.prefix,
                            Stack = (short)c.stack,
                        });
                    }
                    else
                        list.Add(new SyncChestItem()
                        {
                            ChestSlot = 7998,
                            ChestItemSlot = (byte)i,
                            ItemType = 0,
                            Prefix = 0,
                            Stack = 0,
                        });
                });

                args.Player.SendPacket(new SyncPlayerChest()
                {
                    Chest = 7998,
                    Name = chest.name,
                    NameLength = (byte)(chest.name?.Length ?? 0),
                    Position = new((short)chest.X, (short)chest.Y)
                });  //同步箱子信息

                args.Player.SendPackets(list); //同步物品

                args.Player.WatchingChest = null;
            }
        }
        private static void OnCloseChest(BEventArgs.ChestSyncActiveEventArgs args)
        {
            if (GetTopEntity<FakeChest>(args.Position.X, args.Position.Y, args.Player.Index) is { } chest)
                args.Handled = true;
        }
        private static void OnUpdateChest(BEventArgs.ChestUpdateItemEventArgs args)
        {
            args.Handled = args.Player.WatchingChest is null;
        }
        #endregion
        #region OnPreLoadWorld

        private static void OnPreLoadWorld(object? o, HookEvents.Terraria.IO.WorldFile.LoadWorldEventArgs args)
        {
            args.ContinueExecution = false;
            args.OriginalMethod.Invoke(args.loadFromCloud);

            OnPostLoadWorld();
        }

        #endregion
        #region OnPostLoadWorld

        private static void OnPostLoadWorld()
        {
            Console.WriteLine("[FakeProvider] Post load world processing...");
            //FakeProviderAPI.Tile.OffsetX = OffsetX;
            //FakeProviderAPI.Tile.OffsetY = OffsetY;

            Main.maxTilesX = VisibleWidth;
            Main.maxTilesY = VisibleHeight;
            //Main.worldSurface += OffsetY;
            //Main.rockLayer += OffsetY;
            //Main.spawnTileX += OffsetX;
            //Main.spawnTileY += OffsetY;
            WorldGen.setWorldSize();

            lock (ProvidersToAdd)
            {
                ProvidersLoaded = true;

                FakeProviderAPI.Tile.Add(FakeProviderAPI.Tile.Void);
                ProvidersToAdd.Remove(FakeProviderAPI.Tile.Void);

                FakeProviderAPI.Tile.Add(FakeProviderAPI.World);
                ProvidersToAdd.Remove(FakeProviderAPI.World);
                FakeProviderAPI.World.ScanEntities();

                foreach (TileProvider provider in ProvidersToAdd)
                    FakeProviderAPI.Tile.Add(provider);
                ProvidersToAdd.Clear();
            }

            Main.tile = FakeProviderAPI.Tile;

            GC.Collect();
        }

        #endregion
        #region OnPreSaveWorld

        private static void OnPreSaveWorld(WorldSaveEventArgs args)
        {
            if (FakeProviderAPI.World == null)
                return;

            try
            {
                Task.Run(() =>
                {
                    var cloud = false;
                    var time = args.ResetTime;
                    SaveWorld(ref cloud, ref time);
                    Console.WriteLine("[FakeProvier] World saved.");
                });
                args.Handled = true;
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
        }

        #endregion
        #region OnSendData

        private static void OnSendData(SendDataEventArgs args)
        {
            if (args.Handled)
                return;

            switch (args.MsgId)
            {
                case PacketTypes.TileSendSection:
                    args.Handled = true;
                    // We allow sending packet to custom list of players by specifying it in text parameter
                    if (args.text?._text?.Length > 0)
                        SendSectionPacket.Send(args.text._text.Select(c => (int)c), args.ignoreClient,
                            args.number, (int)args.number2, (short)args.number3, (short)args.number4);
                    else
                        SendSectionPacket.Send(args.remoteClient, args.ignoreClient,
                            args.number, (int)args.number2, (short)args.number3, (short)args.number4);
                    break;
                case PacketTypes.TileFrameSection:
                    args.Handled = true;
                    // We allow sending packet to custom list of players by specifying it in text parameter
                    if (args.text?._text?.Length > 0)
                        FrameSectionPacket.Send(args.text._text.Select(c => (int)c), args.ignoreClient,
                            (short)args.number, (short)args.number2, (short)args.number3, (short)args.number4);
                    else
                        FrameSectionPacket.Send(args.remoteClient, args.ignoreClient,
                            (short)args.number, (short)args.number2, (short)args.number3, (short)args.number4);
                    break;
                case PacketTypes.TileSendSquare:
                    args.Handled = true;
                    // We allow sending packet to custom list of players by specifying it in text parameter
                    if (args.text?._text?.Length > 0)
                        SendTileSquarePacket.Send(args.text._text.Select(c => (int)c), args.ignoreClient,
                            (int)args.number3, (int)args.number4, (int)args.number, (int)args.number2, args.number5);
                    else
                        SendTileSquarePacket.Send(args.remoteClient, args.ignoreClient,
                            (int)args.number3, (int)args.number4, (int)args.number, (int)args.number2, args.number5);
                    break;
            }
        }

        #endregion
        #region OnServerLeave

        private static void OnServerLeave(LeaveEventArgs args)
        {
            //FakeProviderAPI.Personal.All(provider => provider.Observers.re)
        }

        #endregion

        #region SendTo

        /*internal static void SendTo(IEnumerable<RemoteClient> clients, byte[] data)
        {
            foreach (RemoteClient client in clients)
                try
                {

                    if (NetSendBytes(client, data, 0, data.Length))
                        return;

                    client.Socket.AsyncSend(data, 0, data.Length,
                        new SocketSendCallback(client.ServerWriteCallBack), null);
                }
                catch (IOException) { }
                catch (ObjectDisposedException) { }
                catch (InvalidOperationException) { }
        }*/
        internal static void SendTo(IEnumerable<RemoteClient> clients, byte[] data)
        {
            foreach (RemoteClient client in clients)
                try
                {
                    client.Socket.AsyncSend(data, 0, data.Length,
                        new SocketSendCallback(client.ServerWriteCallBack), null);
                }
                catch (IOException) { }
                catch (ObjectDisposedException) { }
                catch (InvalidOperationException) { }
        }

        #endregion
        #region FindProvider

        public static bool FindProvider(string name, TSPlayer player, out TileProvider provider, bool includeGlobal = true, bool includePersonal = false)
        {
            provider = null;
            var foundProviders = FakeProviderAPI.FindProvider(name, includeGlobal, includePersonal);

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

        #region FakeCommand

        public static void FakeCommand(CommandArgs args)
        {
            string arg0 = args.Parameters.ElementAtOrDefault(0);
            switch (arg0?.ToLower())
            {
                case "l":
                case "list":
                    {
                        if (!PaginationTools.TryParsePageNumber(args.Parameters, 1, args.Player, out int page))
                            return;

                        List<string> lines = PaginationTools.BuildLinesFromTerms(FakeProviderAPI.Tile.Global);
                        PaginationTools.SendPage(args.Player, page, lines, new PaginationTools.Settings()
                        {
                            HeaderFormat = "Fake providers ({0}/{1}):",
                            FooterFormat = "Type '/fake list {0}' for more.",
                            NothingToDisplayString = "There are no fake providers yet."
                        });
                        break;
                    }
                case "tp":
                case "teleport":
                    {
                        if (args.Parameters.Count != 2)
                        {
                            args.Player.SendErrorMessage("/fake tp \"provider name\"");
                            return;
                        }
                        if (!FindProvider(args.Parameters[1], args.Player, out TileProvider provider))
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
                            args.Player.SendErrorMessage("/fake move \"provider name\" <relative x> <relative y>");
                            return;
                        }
                        if (!FindProvider(args.Parameters[1], args.Player, out TileProvider provider))
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
                            args.Player.SendErrorMessage("/fake layer \"provider name\" <layer>");
                            return;
                        }
                        if (!FindProvider(args.Parameters[1], args.Player, out TileProvider provider))
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
                            args.Player.SendErrorMessage("/fake info \"provider name\"");
                            return;
                        }
                        if (!FindProvider(args.Parameters[1], args.Player, out TileProvider provider))
                            return;

                        args.Player.SendInfoMessage(
    $@"Fake provider '{provider.Name}' ({provider.GetType().Name})
Position and size: {provider.XYWH()}
Enabled: {provider.Enabled}
Entities: {provider.Entities.Count}");
                        break;
                    }
                case "d":
                case "disable":
                    {
                        if (args.Parameters.Count != 2)
                        {
                            args.Player.SendErrorMessage("/fake disable \"provider name\"");
                            return;
                        }
                        if (!FindProvider(args.Parameters[1], args.Player, out TileProvider provider))
                            return;

                        provider.Disable();
                        break;
                    }
                case "e":
                case "enable":
                    {
                        if (args.Parameters.Count != 2)
                        {
                            args.Player.SendErrorMessage("/fake enable \"provider name\"");
                            return;
                        }
                        if (!FindProvider(args.Parameters[1], args.Player, out TileProvider provider))
                            return;

                        provider.Enable();
                        break;
                    }
                default:
                    {
                        args.Player.SendSuccessMessage("/fake subcommands:");
                        args.Player.SendInfoMessage(
    @"/fake info ""provider name""
/fake tp ""provider name""
/fake move ""provider name"" <relative x> <relative y>
/fake layer ""provider name"" <layer>
/fake disable ""provider name""
/fake enable ""provider name""
/fake list [page]");
                        break;
                    }
            }
        }

        #endregion
        #region PersonalFakeCommand
        public static void PersonalFakeCommand(CommandArgs args)
        {
            string arg0 = args.Parameters.ElementAtOrDefault(0);
            switch (arg0?.ToLower())
            {
                case "l":
                case "list":
                    {
                        bool allPersonalProviders = args.Parameters.RemoveAll(s => s == "all") > 0;
                        if (!PaginationTools.TryParsePageNumber(args.Parameters, 1, args.Player, out int page))
                            return;

                        List<string> lines = null;
                        if (allPersonalProviders)
                            lines = PaginationTools.BuildLinesFromTerms(FakeProviderAPI.Tile.Personal);
                        else
                            lines = PaginationTools.BuildLinesFromTerms(FakeProviderAPI.Tile.Personal.Where(provider => provider.Observers.Contains(args.Player.Index)));

                        PaginationTools.SendPage(args.Player, page, lines, new PaginationTools.Settings()
                        {
                            HeaderFormat = "Fake providers ({0}/{1}):",
                            FooterFormat = "Type '/pfake list {0}' for more.",
                            NothingToDisplayString = "There are no personal fake providers yet."
                        });
                        break;
                    }
                case "tp":
                case "teleport":
                    {
                        if (args.Parameters.Count != 2)
                        {
                            args.Player.SendErrorMessage("/fake tp \"provider name\"");
                            return;
                        }
                        if (!FindProvider(args.Parameters[1], args.Player, out TileProvider provider, false, true))
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
                            args.Player.SendErrorMessage("/fake move \"provider name\" <relative x> <relative y>");
                            return;
                        }
                        if (!FindProvider(args.Parameters[1], args.Player, out TileProvider provider, false, true))
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
                            args.Player.SendErrorMessage("/fake layer \"provider name\" <layer>");
                            return;
                        }
                        if (!FindProvider(args.Parameters[1], args.Player, out TileProvider provider, false, true))
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
                            args.Player.SendErrorMessage("/fake info \"provider name\"");
                            return;
                        }
                        if (!FindProvider(args.Parameters[1], args.Player, out TileProvider provider, false, true))
                            return;

                        args.Player.SendInfoMessage(
    $@"Fake provider '{provider.Name}' ({provider.GetType().Name})
Position and size: {provider.XYWH()}
Enabled: {provider.Enabled}
Entities: {provider.Entities.Count}");
                        break;
                    }
                case "d":
                case "disable":
                    {
                        if (args.Parameters.Count != 2)
                        {
                            args.Player.SendErrorMessage("/fake disable \"provider name\"");
                            return;
                        }
                        if (!FindProvider(args.Parameters[1], args.Player, out TileProvider provider, false, true))
                            return;

                        provider.Disable();
                        break;
                    }
                case "e":
                case "enable":
                    {
                        if (args.Parameters.Count != 2)
                        {
                            args.Player.SendErrorMessage("/fake enable \"provider name\"");
                            return;
                        }
                        if (!FindProvider(args.Parameters[1], args.Player, out TileProvider provider, false, true))
                            return;

                        provider.Enable();
                        break;
                    }
                default:
                    {
                        args.Player.SendSuccessMessage("/fake subcommands:");
                        args.Player.SendInfoMessage(
    @"/pfake info ""provider name""
/pfake tp ""provider name""
/pfake move ""provider name"" <relative x> <relative y>
/pfake layer ""provider name"" <layer>
/pfake disable ""provider name""
/pfake enable ""provider name""
/pfake list [page]");
                        break;
                    }
            }
        }
        #endregion

        #region SaveWorld

        private static void SaveWorld(ref bool Cloud, ref bool ResetTime)
        {
            SaveWorldDirect(Cloud, ResetTime);
            //SaveWorldEnd(Cloud, ResetTime);
        }

        #endregion
        #region SaveWorldEnd

        /*private static void SaveWorldEnd(bool useCloudSaving, bool resetTime)
        {
            Hooks.World.IO.PostSaveWorldHandler postSaveWorld = Hooks.World.IO.PostSaveWorld;
            if (postSaveWorld != null)
            {
                postSaveWorld(useCloudSaving, resetTime);
            }
        }*/

        #endregion
        #region SetStatusText

        /*private static void SetStatusText(string text)
        {
            Hooks.Game.StatusTextHandler statusTextWrite = Hooks.Game.StatusTextWrite;
            HookResult? hookResult = (statusTextWrite != null) ? new HookResult?(statusTextWrite(ref text)) : null;
            bool flag = hookResult != null && hookResult.Value == HookResult.Cancel;
            if (!flag)
            {
                StatusTextField.SetValue(null, text);
            }
        }*/
        private static void SetStatusText(string text)
        {
            /*Hooks.Main.StatusTextHandler statusTextWrite = Hooks.Game.StatusTextWrite;
            HookResult? hookResult = (statusTextWrite != null) ? new HookResult?(statusTextWrite(ref text)) : null;
            bool flag = hookResult != null && hookResult.Value == HookResult.Cancel;
            if (!flag)
            {
                StatusTextField.SetValue(null, text);
            }*/
            Main.statusText = text;
        }

        #endregion
        #region SaveWorldDirect

        private static void SaveWorldDirect(bool useCloudSaving, bool resetTime = false)
        {
            if (useCloudSaving && SocialAPI.Cloud == null)
            {
                return;
            }
            if (Main.worldName == "")
            {
                Main.worldName = "World";
            }
            while (WorldGen.IsGeneratingHardMode)
            {
                SetStatusText(Lang.gen[48].Value);
            }
            if (Monitor.TryEnter(WorldFile.IOLock))
            {
                try
                {
                    FileUtilities.ProtectedInvoke(delegate
                    {
                        InternalSaveWorld(useCloudSaving, resetTime);
                    });
                    return;
                }
                finally
                {
                    Monitor.Exit(WorldFile.IOLock);
                }
                return;
            }
        }

        #endregion
        #region InternalSaveWorld

        public static void InternalSaveWorld(bool useCloudSaving, bool resetTime)
        {
            Terraria.Utils.TryCreatingDirectory(Directory.GetParent(Main.worldPathName).FullName);
            if (Main.skipMenu)
            {
                return;
            }
            if (WorldFile._hasCache)
            {
                WorldFile.SetTempToCache();
            }
            else
            {
                WorldFile.SetTempToOngoing();
            }
            if (resetTime)
            {
                WorldFile.ResetTempsToDayTime();
            }
            if (Main.worldPathName == null)
            {
                return;
            }
            byte[] array;
            int num;

#if DEBUG
            byte[] defaultArray;
            int defaultSize;
            using (MemoryStream memoryStream = new MemoryStream(7000000))
            {
                using (BinaryWriter binaryWriter = new BinaryWriter(memoryStream))
                {
                    Main.maxTilesX = FakeProviderAPI.World.Width;
                    Main.maxTilesY = FakeProviderAPI.World.Height;
                    Main.worldSurface -= OffsetY;
                    Main.rockLayer -= OffsetY;
                    Main.tile = FakeProviderAPI.World;
                    FakeProviderAPI.Tile.HideEntities();

                    WorldFile.SaveWorld_Version2(binaryWriter);

                    Main.maxTilesX = VisibleWidth;
                    Main.maxTilesY = VisibleHeight;
                    Main.worldSurface += OffsetY;
                    Main.rockLayer += OffsetY;
                    Main.tile = FakeProviderAPI.Tile;
                    FakeProviderAPI.Tile.UpdateEntities();
                }
                defaultArray = memoryStream.ToArray();
                defaultSize = defaultArray.Length;
            }

#endif

            using (MemoryStream memoryStream = new MemoryStream(7000000))
            {
                using (BinaryWriter binaryWriter = new BinaryWriter(memoryStream))
                {
                    SaveWorld_Version2(binaryWriter);
                }
                array = memoryStream.ToArray();
                num = array.Length;
            }

#if DEBUG
            Debug = $@"World: {Main.worldPathName}
Load size       : {LoadWorldSize}
Default ave size: {defaultSize}
Custom save size: {num}
Default valid: {ValidateWorldData(defaultArray, defaultSize)}
Custom valid : {ValidateWorldData(array, num)}";

            Console.WriteLine("||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||");
            Console.WriteLine(Debug);
            Console.WriteLine("||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||");

#endif

            byte[] array2 = null;
            if (FileUtilities.Exists(Main.worldPathName, useCloudSaving))
            {
                array2 = FileUtilities.ReadAllBytes(Main.worldPathName, useCloudSaving);
            }
            FileUtilities.Write(Main.worldPathName, array, num, useCloudSaving);
            array = FileUtilities.ReadAllBytes(Main.worldPathName, useCloudSaving);
            string text = null;
            using (MemoryStream memoryStream2 = new MemoryStream(array, 0, num, false))
            {
                using (BinaryReader binaryReader = new BinaryReader(memoryStream2))
                {
                    bool valid;
                    try
                    {
                        valid = !Main.validateSaves || WorldFile.ValidateWorld(binaryReader);
                    }
                    catch (Exception e)
                    {
                        valid = false;
                    }
                    if (valid)
                    {
                        if (array2 != null)
                        {
                            text = Main.worldPathName + ".bak";
                            SetStatusText(Lang.gen[50].Value);
                        }
                        WorldFile.DoRollingBackups(text);
                    }
                    else
                    {
                        text = Main.worldPathName;
                        throw new InvalidDataException("Failed to validate world on save");
                    }
                }
            }
            if (text != null && array2 != null)
            {
                FileUtilities.WriteAllBytes(text, array2, useCloudSaving);
            }
        }

        #endregion
        #region ValidateWorldData

        private static bool ValidateWorldData(byte[] array, int size)
        {
            using (MemoryStream memoryStream2 = new MemoryStream(array, 0, size, false))
            {
                using (BinaryReader binaryReader = new BinaryReader(memoryStream2))
                {
                    bool valid;
                    try
                    {
                        valid = !Main.validateSaves || WorldFile.ValidateWorld(binaryReader);
                    }
                    catch (Exception e)
                    {
                        valid = false;
                    }
                    return valid;
                }
            }
        }

        #endregion
        #region SaveWorld_Version2

        public static void SaveWorld_Version2(BinaryWriter writer)
        {
            int[] pointers = new int[]
            {
                WorldFile.SaveFileFormatHeader(writer),
                SaveWorldHeader(writer),
                SaveWorldTiles(writer),
                SaveChests(writer),
                SaveSigns(writer),
                WorldFile.SaveNPCs(writer),
                SaveTileEntities(writer),
                WorldFile.SaveWeightedPressurePlates(writer),
                WorldFile.SaveTownManager(writer),
                WorldFile.SaveBestiary(writer),
                WorldFile.SaveCreativePowers(writer)
            };
            WorldFile.SaveFooter(writer);
            WorldFile.SaveHeaderPointers(writer, pointers);
        }

        #endregion
        #region SaveWorldHeader

        private static int SaveWorldHeader(BinaryWriter writer)
        {
            writer.Write(Main.worldName);
            writer.Write(Main.ActiveWorldFileData.SeedText);
            writer.Write(Main.ActiveWorldFileData.WorldGeneratorVersion);
            writer.Write(Main.ActiveWorldFileData.UniqueId.ToByteArray());
            writer.Write(Main.worldID);
            writer.Write((int)Main.leftWorld);
            writer.Write((int)Main.rightWorld);
            writer.Write((int)Main.topWorld);
            writer.Write((int)Main.bottomWorld);
            writer.Write((int)FakeProviderAPI.World.Height); //
            writer.Write((int)FakeProviderAPI.World.Width); //
            writer.Write(Main.GameMode);
            writer.Write(Main.drunkWorld);
            writer.Write(Main.getGoodWorld);
            writer.Write(Main.tenthAnniversaryWorld);
            writer.Write(Main.dontStarveWorld);
            writer.Write(Main.notTheBeesWorld);
            writer.Write(Main.ActiveWorldFileData.CreationTime.ToBinary());
            writer.Write((byte)Main.moonType);
            writer.Write(Main.treeX[0]);
            writer.Write(Main.treeX[1]);
            writer.Write(Main.treeX[2]);
            writer.Write(Main.treeStyle[0]);
            writer.Write(Main.treeStyle[1]);
            writer.Write(Main.treeStyle[2]);
            writer.Write(Main.treeStyle[3]);
            writer.Write(Main.caveBackX[0]);
            writer.Write(Main.caveBackX[1]);
            writer.Write(Main.caveBackX[2]);
            writer.Write(Main.caveBackStyle[0]);
            writer.Write(Main.caveBackStyle[1]);
            writer.Write(Main.caveBackStyle[2]);
            writer.Write(Main.caveBackStyle[3]);
            writer.Write(Main.iceBackStyle);
            writer.Write(Main.jungleBackStyle);
            writer.Write(Main.hellBackStyle);
            writer.Write(Main.spawnTileX);
            writer.Write(Main.spawnTileY);
            writer.Write(Main.worldSurface);
            writer.Write(Main.rockLayer);
            writer.Write(WorldFile._tempTime);
            writer.Write(WorldFile._tempDayTime);
            writer.Write(WorldFile._tempMoonPhase);
            writer.Write(WorldFile._tempBloodMoon);
            writer.Write(WorldFile._tempEclipse);
            writer.Write(Main.dungeonX);
            writer.Write(Main.dungeonY);
            writer.Write(WorldGen.crimson);
            writer.Write(NPC.downedBoss1);
            writer.Write(NPC.downedBoss2);
            writer.Write(NPC.downedBoss3);
            writer.Write(NPC.downedQueenBee);
            writer.Write(NPC.downedMechBoss1);
            writer.Write(NPC.downedMechBoss2);
            writer.Write(NPC.downedMechBoss3);
            writer.Write(NPC.downedMechBossAny);
            writer.Write(NPC.downedPlantBoss);
            writer.Write(NPC.downedGolemBoss);
            writer.Write(NPC.downedSlimeKing);
            writer.Write(NPC.savedGoblin);
            writer.Write(NPC.savedWizard);
            writer.Write(NPC.savedMech);
            writer.Write(NPC.downedGoblins);
            writer.Write(NPC.downedClown);
            writer.Write(NPC.downedFrost);
            writer.Write(NPC.downedPirates);
            writer.Write(WorldGen.shadowOrbSmashed);
            writer.Write(WorldGen.spawnMeteor);
            writer.Write((byte)WorldGen.shadowOrbCount);
            writer.Write(WorldGen.altarCount);
            writer.Write(Main.hardMode);
            writer.Write(Main.invasionDelay);
            writer.Write(Main.invasionSize);
            writer.Write(Main.invasionType);
            writer.Write(Main.invasionX);
            writer.Write(Main.slimeRainTime);
            writer.Write((byte)Main.sundialCooldown);
            writer.Write(WorldFile._tempRaining);
            writer.Write(WorldFile._tempRainTime);
            writer.Write(WorldFile._tempMaxRain);
            writer.Write(WorldGen.SavedOreTiers.Cobalt);
            writer.Write(WorldGen.SavedOreTiers.Mythril);
            writer.Write(WorldGen.SavedOreTiers.Adamantite);
            writer.Write((byte)WorldGen.treeBG1);
            writer.Write((byte)WorldGen.corruptBG);
            writer.Write((byte)WorldGen.jungleBG);
            writer.Write((byte)WorldGen.snowBG);
            writer.Write((byte)WorldGen.hallowBG);
            writer.Write((byte)WorldGen.crimsonBG);
            writer.Write((byte)WorldGen.desertBG);
            writer.Write((byte)WorldGen.oceanBG);
            writer.Write((int)Main.cloudBGActive);
            writer.Write((short)Main.numClouds);
            writer.Write(Main.windSpeedTarget);
            writer.Write(Main.anglerWhoFinishedToday.Count);
            for (int i = 0; i < Main.anglerWhoFinishedToday.Count; i++)
            {
                writer.Write(Main.anglerWhoFinishedToday[i]);
            }
            writer.Write(NPC.savedAngler);
            writer.Write(Main.anglerQuest);
            writer.Write(NPC.savedStylist);
            writer.Write(NPC.savedTaxCollector);
            writer.Write(NPC.savedGolfer);
            writer.Write(Main.invasionSizeStart);
            writer.Write(WorldFile._tempCultistDelay);
            writer.Write((short)668);
            for (int j = 0; j < 668; j++)
            {
                writer.Write(NPC.killCount[j]);
            }
            writer.Write(Main.IsFastForwardingTime());
            writer.Write(NPC.downedFishron);
            writer.Write(NPC.downedMartians);
            writer.Write(NPC.downedAncientCultist);
            writer.Write(NPC.downedMoonlord);
            writer.Write(NPC.downedHalloweenKing);
            writer.Write(NPC.downedHalloweenTree);
            writer.Write(NPC.downedChristmasIceQueen);
            writer.Write(NPC.downedChristmasSantank);
            writer.Write(NPC.downedChristmasTree);
            writer.Write(NPC.downedTowerSolar);
            writer.Write(NPC.downedTowerVortex);
            writer.Write(NPC.downedTowerNebula);
            writer.Write(NPC.downedTowerStardust);
            writer.Write(NPC.TowerActiveSolar);
            writer.Write(NPC.TowerActiveVortex);
            writer.Write(NPC.TowerActiveNebula);
            writer.Write(NPC.TowerActiveStardust);
            writer.Write(NPC.LunarApocalypseIsUp);
            writer.Write(WorldFile._tempPartyManual);
            writer.Write(WorldFile._tempPartyGenuine);
            writer.Write(WorldFile._tempPartyCooldown);
            writer.Write(WorldFile.TempPartyCelebratingNPCs.Count);
            for (int k = 0; k < WorldFile.TempPartyCelebratingNPCs.Count; k++)
            {
                writer.Write(WorldFile.TempPartyCelebratingNPCs[k]);
            }
            writer.Write(WorldFile._tempSandstormHappening);
            writer.Write(WorldFile._tempSandstormTimeLeft);
            writer.Write(WorldFile._tempSandstormSeverity);
            writer.Write(WorldFile._tempSandstormIntendedSeverity);
            writer.Write(NPC.savedBartender);
            DD2Event.Save(writer);
            writer.Write((byte)WorldGen.mushroomBG);
            writer.Write((byte)WorldGen.underworldBG);
            writer.Write((byte)WorldGen.treeBG2);
            writer.Write((byte)WorldGen.treeBG3);
            writer.Write((byte)WorldGen.treeBG4);
            writer.Write(NPC.combatBookWasUsed);
            writer.Write(WorldFile._tempLanternNightCooldown);
            writer.Write(WorldFile._tempLanternNightGenuine);
            writer.Write(WorldFile._tempLanternNightManual);
            writer.Write(WorldFile._tempLanternNightNextNightIsGenuine);
            WorldGen.TreeTops.Save(writer);
            writer.Write(Main.forceHalloweenForToday);
            writer.Write(Main.forceXMasForToday);
            writer.Write(WorldGen.SavedOreTiers.Copper);
            writer.Write(WorldGen.SavedOreTiers.Iron);
            writer.Write(WorldGen.SavedOreTiers.Silver);
            writer.Write(WorldGen.SavedOreTiers.Gold);
            writer.Write(NPC.boughtCat);
            writer.Write(NPC.boughtDog);
            writer.Write(NPC.boughtBunny);
            writer.Write(NPC.downedEmpressOfLight);
            writer.Write(NPC.downedQueenSlime);
            writer.Write(NPC.downedDeerclops);
            return (int)writer.BaseStream.Position;
        }

        #endregion
        #region SaveWorldTiles

        private static int SaveWorldTiles(BinaryWriter writer)
        {
            byte[] array = new byte[15];
            for (int i = 0; i < FakeProviderAPI.World.Width; i++) //
            {
                float num = (float)i / (float)FakeProviderAPI.World.Width; //
                SetStatusText(Lang.gen[49].Value + " " + ((int)(num * 100f + 1f)).ToString() + "%");
                for (int j = 0; j < FakeProviderAPI.World.Height; j++) //
                {
                    ITile tile = FakeProviderAPI.World[i, j]; //
                    int num2 = 3;
                    byte b3;
                    byte b2;
                    byte b = b2 = (b3 = 0);
                    bool flag = false;
                    if (tile.active())
                    {
                        flag = true;
                    }
                    if (flag)
                    {
                        b2 |= 2;
                        array[num2] = (byte)tile.type;
                        num2++;
                        if (tile.type > 255)
                        {
                            array[num2] = (byte)(tile.type >> 8);
                            num2++;
                            b2 |= 32;
                        }
                        if (Main.tileFrameImportant[(int)tile.type])
                        {
                            array[num2] = (byte)(tile.frameX & 255);
                            num2++;
                            array[num2] = (byte)(((int)tile.frameX & 65280) >> 8);
                            num2++;
                            array[num2] = (byte)(tile.frameY & 255);
                            num2++;
                            array[num2] = (byte)(((int)tile.frameY & 65280) >> 8);
                            num2++;
                        }
                        if (tile.color() != 0)
                        {
                            b3 |= 8;
                            array[num2] = tile.color();
                            num2++;
                        }
                    }
                    if (tile.wall != 0)
                    {
                        b2 |= 4;
                        array[num2] = (byte)tile.wall;
                        num2++;
                        if (tile.wallColor() != 0)
                        {
                            b3 |= 16;
                            array[num2] = tile.wallColor();
                            num2++;
                        }
                    }
                    if (tile.liquid != 0)
                    {
                        if (tile.lava())
                        {
                            b2 |= 16;
                        }
                        else if (tile.honey())
                        {
                            b2 |= 24;
                        }
                        else
                        {
                            b2 |= 8;
                        }
                        array[num2] = tile.liquid;
                        num2++;
                    }
                    if (tile.wire())
                    {
                        b |= 2;
                    }
                    if (tile.wire2())
                    {
                        b |= 4;
                    }
                    if (tile.wire3())
                    {
                        b |= 8;
                    }
                    int num3;
                    if (tile.halfBrick())
                    {
                        num3 = 16;
                    }
                    else if (tile.slope() != 0)
                    {
                        num3 = (int)(tile.slope() + 1) << 4;
                    }
                    else
                    {
                        num3 = 0;
                    }
                    b |= (byte)num3;
                    if (tile.actuator())
                    {
                        b3 |= 2;
                    }
                    if (tile.inActive())
                    {
                        b3 |= 4;
                    }
                    if (tile.wire4())
                    {
                        b3 |= 32;
                    }
                    if (tile.wall > 255)
                    {
                        array[num2] = (byte)(tile.wall >> 8);
                        num2++;
                        b3 |= 64;
                    }
                    int num4 = 2;
                    if (b3 != 0)
                    {
                        b |= 1;
                        array[num4] = b3;
                        num4--;
                    }
                    if (b != 0)
                    {
                        b2 |= 1;
                        array[num4] = b;
                        num4--;
                    }
                    short num5 = 0;
                    int num6 = j + 1;
                    int num7 = FakeProviderAPI.World.Height - j - 1; //
                    while (num7 > 0 && tile.isTheSameAs(FakeProviderAPI.World[i, num6]) && TileID.Sets.AllowsSaveCompressionBatching[(int)tile.type]) //
                    {
                        num5 += 1;
                        num7--;
                        num6++;
                    }
                    j += (int)num5;
                    if (num5 > 0)
                    {
                        array[num2] = (byte)(num5 & 255);
                        num2++;
                        if (num5 > 255)
                        {
                            b2 |= 128;
                            array[num2] = (byte)(((int)num5 & 65280) >> 8);
                            num2++;
                        }
                        else
                        {
                            b2 |= 64;
                        }
                    }
                    array[num4] = b2;
                    writer.Write(array, num4, num2 - num4);
                }
            }
            return (int)writer.BaseStream.Position;
        }


        #endregion
        #region SaveChests

        private static int SaveChests(BinaryWriter writer)
        {
            short num = 0;
            for (int i = 0; i < 8000; i++)
            {
                Chest chest = Main.chest[i];
                if (chest != null)
                {
                    if (chest is IFake fchest && fchest.Provider != FakeProviderAPI.World) //
                        continue; //
                    bool flag = false;
                    for (int j = chest.x; j <= chest.x + 1; j++)
                    {
                        for (int k = chest.y; k <= chest.y + 1; k++)
                        {
                            if (j < 0 || k < 0 || j >= FakeProviderAPI.World.Width || k >= FakeProviderAPI.World.Height) //
                            {
                                flag = true;
                                break;
                            }
                            ITile tile = FakeProviderAPI.World[j, k]; //
                            if (!tile.active() || !Main.tileContainer[(int)tile.type])
                            {
                                flag = true;
                                break;
                            }
                        }
                    }
                    if (flag)
                    {
                        Main.chest[i] = null;
                    }
                    else
                    {
                        num += 1;
                    }
                }
            }
            writer.Write(num);
            writer.Write((short)40);
            for (int i = 0; i < 8000; i++)
            {
                Chest chest = Main.chest[i];

                if (chest != null)
                {
                    if (chest is IFake fchest && fchest.Provider != FakeProviderAPI.World) //
                        continue; //
                    writer.Write(chest.x);
                    writer.Write(chest.y);
                    writer.Write(chest.name);
                    for (int l = 0; l < 40; l++)
                    {
                        Item item = chest.item[l];
                        if (item == null)
                        {
                            writer.Write((short)0);
                        }
                        else
                        {
                            if (item.stack > item.maxStack)
                            {
                                item.stack = item.maxStack;
                            }
                            if (item.stack < 0)
                            {
                                item.stack = 1;
                            }
                            writer.Write((short)item.stack);
                            if (item.stack > 0)
                            {
                                writer.Write(item.netID);
                                writer.Write(item.prefix);
                            }
                        }
                    }
                }
            }
            return (int)writer.BaseStream.Position;
        }


        #endregion
        #region SaveSigns

        private static int SaveSigns(BinaryWriter writer)
        {
            short num = 0;
            for (int i = 0; i < 1000; i++)
            {
                Sign sign = Main.sign[i];
                if (sign != null && sign.text != null)
                {
                    if (sign is IFake fsign && fsign.Provider != FakeProviderAPI.World) //
                        continue; //
                    num += 1;
                }
            }
            writer.Write(num);
            for (int j = 0; j < 1000; j++)
            {
                Sign sign = Main.sign[j];
                if (sign != null && sign.text != null)
                {
                    if (sign is IFake fsign && fsign.Provider != FakeProviderAPI.World) //
                        continue; //
                    writer.Write(sign.text);
                    writer.Write(sign.x);
                    writer.Write(sign.y);
                }
            }
            return (int)writer.BaseStream.Position;
        }


        #endregion
        #region SaveTileEntities

        private static int SaveTileEntities(BinaryWriter writer)
        {
            object entityCreationLock = TileEntity.EntityCreationLock;
            lock (entityCreationLock)
            {
                writer.Write((int)TileEntity.ByID.Count(keyValuePair => //
                    !(keyValuePair.Value is IFake fentity && fentity.Provider != FakeProviderAPI.World))); //
                foreach (KeyValuePair<int, TileEntity> keyValuePair in TileEntity.ByID)
                {
                    if (keyValuePair.Value is IFake fentity && fentity.Provider != FakeProviderAPI.World) //
                        continue; //
                    TileEntity.Write(writer, keyValuePair.Value, false);
                }
            }
            return (int)writer.BaseStream.Position;
        }


        #endregion

        #region CreateCustomTileProvider

        private static void CreateCustomTileProvider()
        {
            int maxTilesX = Main.maxTilesX;
            int maxTilesY = Main.maxTilesY;
            if (VisibleWidth < 0)
                VisibleWidth = (OffsetX + maxTilesX);
            else
                VisibleWidth++;
            if (VisibleHeight < 0)
                VisibleHeight = (OffsetY + maxTilesY);
            else
                VisibleHeight++;
            FakeProviderAPI.Tile = new TileProviderCollection();
            FakeProviderAPI.Tile.Initialize(VisibleWidth, VisibleHeight);

            FakeProviderAPI.World = FakeProviderAPI.CreateTileProvider(FakeProviderAPI.WorldProviderName, 0, 0,
                maxTilesX, maxTilesY, Int32.MinValue + 1);

            using (IDisposable previous = Main.tile as IDisposable)
                Main.tile = FakeProviderAPI.World;
        }

        #endregion
        #region LoadWorldDirect

        private static void LoadWorldDirect(bool loadFromCloud)
        {
            Main.lockMenuBGChange = true;
            WorldFile._isWorldOnCloud = loadFromCloud;
            Main.checkXMas();
            Main.checkHalloween();
            bool flag = loadFromCloud && SocialAPI.Cloud != null;
            if (!FileUtilities.Exists(Main.worldPathName, flag) && Main.autoGen)
            {
                if (!flag)
                {
                    for (int i = Main.worldPathName.Length - 1; i >= 0; i--)
                    {
                        if (Main.worldPathName.Substring(i, 1) == (Path.DirectorySeparatorChar.ToString() ?? ""))
                        {
                            Terraria.Utils.TryCreatingDirectory(Directory.GetParent(Main.worldPathName.Substring(0, i)).FullName);
                            break;
                        }
                    }
                }
                WorldGen.clearWorld();
                Main.ActiveWorldFileData = WorldFile.CreateMetadata((Main.worldName == "") ? "World" : Main.worldName, flag, Main.GameMode);
                string text = (Main.AutogenSeedName ?? "").Trim();
                if (text.Length == 0)
                {
                    Main.ActiveWorldFileData.SetSeedToRandom();
                }
                else
                {
                    Main.ActiveWorldFileData.SetSeed(text);
                }
                WorldGen.GenerateWorld(Main.ActiveWorldFileData.Seed, Main.AutogenProgress);
                WorldFile.SaveWorld();
            }
            using (MemoryStream memoryStream = new MemoryStream(FileUtilities.ReadAllBytes(Main.worldPathName, flag)))
            {
                using (BinaryReader binaryReader = new BinaryReader(memoryStream))
                {
                    try
                    {
                        WorldGen.loadFailed = false;
                        WorldGen.loadSuccess = false;
                        int num = WorldFile._versionNumber = binaryReader.ReadInt32();
                        int num2;
                        if (num <= 87)
                        {
                            num2 = WorldFile.LoadWorld_Version1_Old_BeforeRelease88(binaryReader);
                        }
                        else
                        {
                            num2 = LoadWorld_Version2(binaryReader);
                        }
                        if (num < 141)
                        {
                            if (!loadFromCloud)
                            {
                                Main.ActiveWorldFileData.CreationTime = File.GetCreationTime(Main.worldPathName);
                            }
                            else
                            {
                                Main.ActiveWorldFileData.CreationTime = DateTime.Now;
                            }
                        }
                        WorldFile.CheckSavedOreTiers();

                        LoadWorldSize = binaryReader.BaseStream.Position; //DEBUG

                        binaryReader.Close();
                        memoryStream.Close();
                        if (num2 != 0)
                        {
                            WorldGen.loadFailed = true;
                        }
                        else
                        {
                            WorldGen.loadSuccess = true;
                        }
                        if (WorldGen.loadFailed || !WorldGen.loadSuccess)
                        {
                            MessageBox((IntPtr)0, $"Failed loading world ({Main.worldPathName}): (loadFailed || !WorldGen.loadSuccess)", "TerrariaServer startup error", 0);
                            Environment.Exit(1);
                        }
                        WorldFile.ConvertOldTileEntities();
                        WorldFile.ClearTempTiles();
                        WorldGen.gen = true;
                        //WorldGen.waterLine = Main.maxTilesY;
                        Liquid.QuickWater(2, -1, -1);
                        WorldGen.WaterCheck();
                        int num3 = 0;
                        Liquid.quickSettle = true;
                        int num4 = Liquid.numLiquid + LiquidBuffer.numLiquidBuffer;
                        float num5 = 0f;
                        while (Liquid.numLiquid > 0 && num3 < 100000)
                        {
                            num3++;
                            float num6 = (float)(num4 - (Liquid.numLiquid + LiquidBuffer.numLiquidBuffer)) / (float)num4;
                            if (Liquid.numLiquid + LiquidBuffer.numLiquidBuffer > num4)
                            {
                                num4 = Liquid.numLiquid + LiquidBuffer.numLiquidBuffer;
                            }
                            if (num6 > num5)
                            {
                                num5 = num6;
                            }
                            else
                            {
                                num6 = num5;
                            }
                            SetStatusText(string.Concat(new object[]
                            {
                                Lang.gen[27].Value,
                                " ",
                                (int)(num6 * 100f / 2f + 50f),
                                "%"
                            }));
                            Liquid.UpdateLiquid();
                        }
                        Liquid.quickSettle = false;
                        Main.weatherCounter = WorldGen.genRand.Next(3600, 18000);
                        Cloud.resetClouds();
                        WorldGen.WaterCheck();
                        WorldGen.gen = false;
                        NPC.setFireFlyChance();
                        if (Main.slimeRainTime > 0.0)
                        {
                            Main.StartSlimeRain(false);
                        }
                        NPC.SetWorldSpecificMonstersByWorldID();
                    }
                    catch (Exception value)
                    {
                        WorldGen.loadFailed = true;
                        WorldGen.loadSuccess = false;
                        System.Console.WriteLine(value);
                        try
                        {
                            binaryReader.Close();
                            memoryStream.Close();
                        }
                        catch
                        {
                        }
                        MessageBox((IntPtr)0, $"Failed loading world ({Main.worldPathName}):\n{value}", "TerrariaServer startup error", 0);
                        Environment.Exit(1);
                    }
                }
            }

            EventInfo eventOnWorldLoad = typeof(WorldFile).GetEvent("OnWorldLoad", BindingFlags.Public | BindingFlags.Static);
            eventOnWorldLoad.GetRaiseMethod()?.Invoke(null, new object[] { });
            //if (WorldFile.OnWorldLoad != null)
            //WorldFile.OnWorldLoad();
        }


        #endregion
        #region LoadWorld_Version2

        private static int LoadWorld_Version2(BinaryReader reader)
        {
            reader.BaseStream.Position = 0L;
            bool[] importance;
            int[] array;
            if (!WorldFile.LoadFileFormatHeader(reader, out importance, out array))
            {
                return 5;
            }
            if (reader.BaseStream.Position != (long)array[0])
            {
                return 5;
            }
            WorldFile.LoadHeader(reader);
            if (reader.BaseStream.Position != (long)array[1])
            {
                return 5;
            }

            // ======================
            CreateCustomTileProvider();
            // ======================

            WorldFile.LoadWorldTiles(reader, importance);
            if (reader.BaseStream.Position != (long)array[2])
            {
                return 5;
            }
            WorldFile.LoadChests(reader);
            if (reader.BaseStream.Position != (long)array[3])
            {
                return 5;
            }
            WorldFile.LoadSigns(reader);
            if (reader.BaseStream.Position != (long)array[4])
            {
                return 5;
            }
            WorldFile.LoadNPCs(reader);
            if (reader.BaseStream.Position != (long)array[5])
            {
                return 5;
            }
            if (WorldFile._versionNumber >= 116)
            {
                if (WorldFile._versionNumber < 122)
                {
                    WorldFile.LoadDummies(reader);
                    if (reader.BaseStream.Position != (long)array[6])
                    {
                        return 5;
                    }
                }
                else
                {
                    WorldFile.LoadTileEntities(reader);
                    if (reader.BaseStream.Position != (long)array[6])
                    {
                        return 5;
                    }
                }
            }
            if (WorldFile._versionNumber >= 170)
            {
                WorldFile.LoadWeightedPressurePlates(reader);
                if (reader.BaseStream.Position != (long)array[7])
                {
                    return 5;
                }
            }
            if (WorldFile._versionNumber >= 189)
            {
                WorldFile.LoadTownManager(reader);
                if (reader.BaseStream.Position != (long)array[8])
                {
                    return 5;
                }
            }
            if (WorldFile._versionNumber >= 210)
            {
                WorldFile.LoadBestiary(reader, WorldFile._versionNumber);
                if (reader.BaseStream.Position != (long)array[9])
                {
                    return 5;
                }
            }
            else
            {
                WorldFile.LoadBestiaryForVersionsBefore210();
            }
            if (WorldFile._versionNumber >= 220)
            {
                WorldFile.LoadCreativePowers(reader, WorldFile._versionNumber);
                if (reader.BaseStream.Position != (long)array[10])
                {
                    return 5;
                }
            }
            return WorldFile.LoadFooter(reader);
        }

        #endregion
    }
}

