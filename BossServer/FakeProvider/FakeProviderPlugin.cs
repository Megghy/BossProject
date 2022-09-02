#region Using
using BossFramework;
using BossFramework.BCore;
using BossFramework.BModels;
using Microsoft.Xna.Framework;
using OTAPI;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Terraria;
using Terraria.DataStructures;
using Terraria.GameContent;
using Terraria.GameContent.Creative;
using Terraria.GameContent.Events;
using Terraria.GameContent.Tile_Entities;
using Terraria.ID;
using Terraria.IO;
using Terraria.Net.Sockets;
using Terraria.Social;
using Terraria.Utilities;
using TerrariaApi.Server;
using TrProtocol;
using TrProtocol.Packets;
using TShockAPI;
using static Terraria.GameContent.Creative.CreativePowers;
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
            Order = -1002;
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

            ServerApi.Hooks.WorldLoad.Register(this, OnPreLoadWorld);
            ServerApi.Hooks.PostWorldLoad.Register(this, OnPostLoadWorld);
            ServerApi.Hooks.WorldSave.Register(this, OnPreSaveWorld);
            ServerApi.Hooks.NetSendData.Register(this, OnSendData, Int32.MaxValue);
            ServerApi.Hooks.ServerLeave.Register(this, OnServerLeave);

            SignRedirector.SignRead += OnRequestSign;
            SignRedirector.SignUpdate += OnUpdateSign;
            ChestRedirector.ChestOpen += OnRequestChest;
            ChestRedirector.ChestSyncActive += OnCloseChest;
            ChestRedirector.ChestUpdateItem += OnUpdateChest;

            Commands.ChatCommands.AddRange(CommandList);
        }

        #endregion
        #region Dispose

        protected override void Dispose(bool Disposing)
        {
            if (Disposing)
            {
                ServerApi.Hooks.WorldLoad.Deregister(this, OnPreLoadWorld);
                ServerApi.Hooks.PostWorldLoad.Deregister(this, OnPostLoadWorld);
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
                40.ForEach(i =>
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

        private static void OnPreLoadWorld(HandledEventArgs args)
        {
            args.Handled = true;
            if (FastWorldLoad) 
                LoadWorldFast();
            else 
                LoadWorldDirect(false);
            TerrariaApi.Server.Hooking.WorldHooks._hookManager.InvokePostWorldLoad();
        }

        #endregion
        #region OnPostLoadWorld

        private static void OnPostLoadWorld(HandledEventArgs args)
        {
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
                {client.Socket.AsyncSend(data, 0, data.Length,
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
            writer.Write(Main.fastForwardTime);
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
                        WorldGen.waterLine = Main.maxTilesY;
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

        #region LoadWorldFast
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int Index(int x, int y) => x * Main.tile.Height + y;

        private unsafe static void LoadWorldFast()
        {
            Console.WriteLine("[FakeProvider] Loading World using FastLoadWorld");
            try
            {
                System.Diagnostics.Stopwatch sw = new System.Diagnostics.Stopwatch();
                sw.Start();
                WorldGen.loadFailed = false;
                WorldGen.loadSuccess = true;

                //TODO: see if we want these
                //Main.checkXMas();
                //Main.checkHalloween();

                //Autogenerate world if flag set (Not optimized)
                #region AutoGen
                if (!File.Exists(Main.worldPathName) && Main.autoGen)
                {
                    Terraria.Utils.TryCreatingDirectory(Directory.GetParent(Main.worldPathName).FullName);
                    WorldGen.clearWorld();
                    Main.ActiveWorldFileData = WorldFile.CreateMetadata((Main.worldName == "") ? "World" : Main.worldName, false, Main.GameMode);
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
                #endregion

                #region WorldLoad
                string debugPointerString = "Before FormatHeader";
                using UnsafeBinaryReader reader =
                    new UnsafeBinaryReader(File.ReadAllBytes(Main.worldPathName));
                try
                {
                    WorldGen.loadFailed = false;
                    WorldGen.loadSuccess = false;

                    int versionNumber = reader.ReadInt32();
                    // <= 87 to not support really old versions I mean, when are we loading these realistically
                    if (versionNumber <= 87 || versionNumber > Main.curRelease)
                    {
                        WorldGen.loadFailed = true;
                        throw new Exception("Invalid world file version");
                    }
                    #region MainLoadWorld
                    int loadWorldRet = 0;
                    //TODO: readd integrity verificator using format header values but with unsafebinaryreader?
                    //reader.BaseStream.Position = 0L;
                    /*if (!LoadFileFormatHeader(reader, out var importance, out var positions))
					{
						loadWorldRet = 5;
						goto WORLDLOAD_END;
					}*/
                    #region LoadFileFormatHeader
                    bool[] importance = null;
                    int[] positions = null;
                    Console.WriteLine($"[FakeProvider] WorldVersion: {versionNumber}");
                    if (versionNumber >= 135)
                    {
                        try
                        {
                            #region FileMetadata.Read(reader, FileType.World)
                            FileType expectedType = FileType.World;
                            Main.WorldFileMetadata = new FileMetadata();
                            #region fileMetadata.Read()
                            ulong fileMetadatanum = reader.ReadUInt64();

                            if ((fileMetadatanum & 0xFFFFFFFFFFFFFFL) != 27981915666277746L)
                            {
                                throw new FormatException("Expected Re-Logic file format.");
                            }
                            byte b1 = (byte)((fileMetadatanum >> 56) & 0xFF);
                            FileType fileType = FileType.None;
                            FileType[] array = (FileType[])Enum.GetValues(typeof(FileType));
                            for (int i = 0; i < array.Length; i++)
                            {
                                if ((uint)array[i] == b1)
                                {
                                    fileType = array[i];
                                    break;
                                }
                            }
                            if (fileType == FileType.None)
                            {
                                throw new FormatException("Found invalid file type.");
                            }
                            Main.WorldFileMetadata.Type = fileType;
                            Main.WorldFileMetadata.Revision = reader.ReadUInt32();
                            ulong fileMetadataNum2 = reader.ReadUInt64();
                            Main.WorldFileMetadata.IsFavorite = (fileMetadataNum2 & 1) == 1;
                            #endregion
                            if (Main.WorldFileMetadata.Type != expectedType)
                            {
                                throw new FormatException("Expected type \"" + Enum.GetName(typeof(FileType), expectedType) + "\" but found \"" + Enum.GetName(typeof(FileType), Main.WorldFileMetadata.Type) + "\".");
                            }
                            #endregion
                        }
                        catch (FormatException value)
                        {
                            Console.WriteLine(Terraria.Localization.Language.GetTextValue("Error.UnableToLoadWorld"));
                            Console.WriteLine(value);
                            Console.ReadLine();
                            return;
                        }
                    }
                    else
                    {
                        Main.WorldFileMetadata = FileMetadata.FromCurrentSettings(FileType.World);
                    }
                    short headerPositionsNum = reader.ReadInt16();
                    positions = new int[headerPositionsNum];
                    for (int hPos = 0; hPos < headerPositionsNum; hPos++)
                    {
                        positions[hPos] = reader.ReadInt32();
                    }
                    short importanceNum = reader.ReadInt16();
                    importance = new bool[importanceNum];
                    byte headerByte = 0;
                    byte headerByte2 = 128;
                    for (int i = 0; i < importanceNum; i++)
                    {
                        if (headerByte2 == 128)
                        {
                            headerByte = reader.ReadByte();
                            headerByte2 = 1;
                        }
                        else
                        {
                            headerByte2 = (byte)(headerByte2 << 1);
                        }
                        if ((headerByte & headerByte2) == headerByte2)
                        {
                            importance[i] = true;
                        }
                    }
                    #endregion
                    debugPointerString = "After FormatHeader";
                    /*if (reader.BaseStream.Position != positions[0])
					{
						loadWorldRet = 5;
                        goto WORLDLOAD_END;
					}*/
                    #region LoadHeader
                    Main.worldName = reader.ReadString();
                    string positionString = "";
                    for (int str = 0; str < positions.Length; str++)
                    {
                        positionString += $"{positions[str]} ";
                    }
                    //Header byte counts, TODO: readd fail checks with these?
                    //Console.WriteLine($"{Main.worldName} positions: {positionString}");
                    if (versionNumber >= 179)
                    {
                        string seed = ((versionNumber != 179) ? reader.ReadString() : reader.ReadInt32().ToString());
                        Main.ActiveWorldFileData.SetSeed(seed);
                        Main.ActiveWorldFileData.WorldGeneratorVersion = reader.ReadUInt64();
                    }
                    if (versionNumber >= 181)
                    {
                        Main.ActiveWorldFileData.UniqueId = new Guid(reader.ReadBytes(16));
                    }
                    else
                    {
                        Main.ActiveWorldFileData.UniqueId = Guid.NewGuid();
                    }
                    Main.worldID = reader.ReadInt32();
                    Main.leftWorld = reader.ReadInt32();
                    Main.rightWorld = reader.ReadInt32();
                    Main.topWorld = reader.ReadInt32();
                    Main.bottomWorld = reader.ReadInt32();
                    Main.maxTilesY = reader.ReadInt32();
                    Main.maxTilesX = reader.ReadInt32();
                    #region clearWorld()
                    //	Main.ladyBugRainBoost = 0;
                    //	Main.getGoodWorld = false;
                    //	Main.drunkWorld = false;
                    //	//Main.tenthAnniversaryWorld = false;
                    //	NPC.ResetBadgerHatTime();
                    //	NPC.freeCake = false;
                    //	Main.mapDelay = 2;
                    //	Main.ResetWindCounter(resetExtreme: true);
                    //  WorldGen.TownManager = new TownRoomManager();
                    //	//Hooks.ClearWorld();
                    //	TileEntity.Clear();
                    //	Main.checkXMas();
                    //	Main.checkHalloween();
                    //	/*if (Main.mapReady)
                    //	{
                    //		for (int i = 0; i < lastMaxTilesX; i++)
                    //		{
                    //			_ = (float)i / (float)lastMaxTilesX;
                    //			Main.statusText = Lang.gen[65].Value;
                    //		}
                    //		if (Main.Map != null)
                    //		{
                    //			Main.Map.Clear();
                    //		}
                    //	}*/
                    //	if (Main.mapReady)
                    //		Main.Map.Clear();
                    //	NPC.MoonLordCountdown = 0;
                    //	Main.forceHalloweenForToday = false;
                    //	Main.forceXMasForToday = false;
                    //	NPC.RevengeManager.Reset();
                    //	Main.pumpkinMoon = false;
                    //	Main.clearMap = true;
                    //	Main.mapTime = 0;
                    //	Main.updateMap = false;
                    //	Main.mapReady = false;
                    //	Main.refreshMap = false;
                    //	Main.eclipse = false;
                    //	Main.slimeRain = false;
                    //	Main.slimeRainTime = 0.0;
                    //	Main.slimeWarningTime = 0;
                    //	Main.sundialCooldown = 0;
                    //	Main.fastForwardTime = false;
                    //	BirthdayParty.WorldClear();
                    //	LanternNight.WorldClear();
                    //	//mysticLogsEvent.WorldClear();
                    //	CreditsRollEvent.Reset();
                    //	Sandstorm.WorldClear();
                    //	Main.UpdateTimeRate();
                    //	Main.wofNPCIndex = -1;
                    //	NPC.waveKills = 0f;
                    //	/*spawnHardBoss = 0;
                    //	totalSolid2 = 0;
                    //	totalGood2 = 0;
                    //	totalEvil2 = 0;
                    //	totalBlood2 = 0;
                    //	totalSolid = 0;
                    //	totalGood = 0;
                    //	totalEvil = 0;
                    //	totalBlood = 0;*/
                    //	WorldFile.ResetTemps();
                    //	Main.maxRaining = 0f;
                    //	/*totalX = 0;
                    //	totalD = 0;
                    //	tEvil = 0;
                    //	tBlood = 0;
                    //	tGood = 0;
                    //	spawnEye = false;
                    //	prioritizedTownNPCType = 0;
                    //	shadowOrbCount = 0;
                    //	altarCount = 0;
                    //	SavedOreTiers.Copper = -1;
                    //	SavedOreTiers.Iron = -1;
                    //	SavedOreTiers.Silver = -1;
                    //	SavedOreTiers.Gold = -1;
                    //	SavedOreTiers.Cobalt = -1;
                    //	SavedOreTiers.Mythril = -1;
                    //	SavedOreTiers.Adamantite = -1;*/
                    //	Main.cloudBGActive = 0f;
                    //	Main.raining = false;
                    //	Main.hardMode = false;
                    //	Main.helpText = 0;
                    //	Main.BartenderHelpTextIndex = 0;
                    //	Main.dungeonX = 0;
                    //	Main.dungeonY = 0;
                    //	NPC.downedBoss1 = false;
                    //	NPC.downedBoss2 = false;
                    //	NPC.downedBoss3 = false;
                    //	NPC.downedQueenBee = false;
                    //	NPC.downedSlimeKing = false;
                    //	NPC.downedMechBossAny = false;
                    //	NPC.downedMechBoss1 = false;
                    //	NPC.downedMechBoss2 = false;
                    //	NPC.downedMechBoss3 = false;
                    //	NPC.downedFishron = false;
                    //	NPC.downedAncientCultist = false;
                    //	NPC.downedMoonlord = false;
                    //	NPC.downedHalloweenKing = false;
                    //	NPC.downedHalloweenTree = false;
                    //	NPC.downedChristmasIceQueen = false;
                    //	NPC.downedChristmasSantank = false;
                    //	NPC.downedChristmasTree = false;
                    //	NPC.downedPlantBoss = false;
                    //	NPC.downedGolemBoss = false;
                    //	NPC.downedEmpressOfLight = false;
                    //	NPC.downedQueenSlime = false;
                    //	NPC.combatBookWasUsed = false;
                    //	NPC.savedStylist = false;
                    //	NPC.savedGoblin = false;
                    //	NPC.savedWizard = false;
                    //	NPC.savedMech = false;
                    //	NPC.savedTaxCollector = false;
                    //	NPC.savedAngler = false;
                    //	NPC.savedBartender = false;
                    //	NPC.savedGolfer = false;
                    //	NPC.boughtCat = false;
                    //	NPC.boughtDog = false;
                    //	NPC.boughtBunny = false;
                    //	NPC.downedGoblins = false;
                    //	NPC.downedClown = false;
                    //	NPC.downedFrost = false;
                    //	NPC.downedPirates = false;
                    //	NPC.downedMartians = false;
                    //	NPC.downedTowerSolar = (NPC.downedTowerVortex = (NPC.downedTowerNebula = (NPC.downedTowerStardust = (NPC.LunarApocalypseIsUp = false))));
                    //	NPC.TowerActiveSolar = (NPC.TowerActiveVortex = (NPC.TowerActiveNebula = (NPC.TowerActiveStardust = false)));
                    //	DD2Event.ResetProgressEntirely();
                    //	NPC.ClearFoundActiveNPCs();
                    //	Main.BestiaryTracker.Reset();
                    //	Main.PylonSystem.Reset();
                    //	CreativePowerManager.Instance.Reset();
                    //	Main.CreativeMenu.Reset();
                    //	//shadowOrbSmashed = false;
                    //	//spawnMeteor = false;
                    //	//stopDrops = false;
                    //	Main.invasionDelay = 0;
                    //	Main.invasionType = 0;
                    //	Main.invasionSize = 0;
                    //	Main.invasionWarn = 0;
                    //	Main.invasionX = 0.0;
                    //	Main.invasionSizeStart = 0;
                    //	Main.treeX[0] = Main.maxTilesX;
                    //	Main.treeX[1] = Main.maxTilesX;
                    //	Main.treeX[2] = Main.maxTilesX;
                    //	Main.treeStyle[0] = 0;
                    //	Main.treeStyle[1] = 0;
                    //	Main.treeStyle[2] = 0;
                    //	Main.treeStyle[3] = 0;
                    //	//noLiquidCheck = false;
                    //	Liquid.numLiquid = 0;
                    //	LiquidBuffer.numLiquidBuffer = 0;
                    ///*	if (Main.netMode == 1 || lastMaxTilesX > Main.maxTilesX || lastMaxTilesY > Main.maxTilesY)
                    //	{
                    //		for (int j = 0; j < lastMaxTilesX; j++)
                    //		{
                    //			//float num = (float)j / (float)lastMaxTilesX;
                    //			//Main.statusText = Lang.gen[46].Value + " " + (int)(num * 100f + 1f) + "%";
                    //			for (int k = 0; k < lastMaxTilesY; k++)
                    //			{
                    //				Main.tile[j, k] = null;
                    //			}
                    //		}
                    //	}*/
                    WorldGen.lastMaxTilesX = Main.maxTilesX;
                    WorldGen.lastMaxTilesY = Main.maxTilesY;
                    //	if (Main.netMode != 2)
                    //	{
                    //		Main.sectionManager = new WorldSections(Main.maxTilesX / 200, Main.maxTilesY / 150);
                    //	}
                    //	if (Main.netMode != 1)
                    //	{
                    //		for (int l = 0; l < Main.maxTilesX; l++)
                    //		{
                    //			//float num2 = (float)l / (float)Main.maxTilesX;
                    //			//Main.statusText = Lang.gen[47].Value + " " + (int)(num2 * 100f + 1f) + "%";
                    //			for (int m = 0; m < Main.maxTilesY; m++)
                    //			{
                    //				tiles[Index(l, m)] = new StructTile();
                    //				/*if (Main.tile[l, m] == null)
                    //				{
                    //					Main.tile[l, m] = new Tile();
                    //				}
                    //				else
                    //				{
                    //					Main.tile[l, m].ClearEverything();
                    //				}*/
                    //			}
                    //		}
                    //	}
                    //	for (int n = 0; n < Main.countsAsHostForGameplay.Length; n++)
                    //	{
                    //		Main.countsAsHostForGameplay[n] = false;
                    //	}
                    //	CombatText.clearAll();
                    //	for (int num3 = 0; num3 < 6000; num3++)
                    //	{
                    //		Main.dust[num3] = new Dust();
                    //		Main.dust[num3].dustIndex = num3;
                    //	}
                    //	for (int num4 = 0; num4 < 600; num4++)
                    //	{
                    //		Main.gore[num4] = new Gore();
                    //	}
                    //	for (int num5 = 0; num5 < 400; num5++)
                    //	{
                    //		Main.item[num5] = new Item();
                    //		Main.timeItemSlotCannotBeReusedFor[num5] = 0;
                    //	}
                    //	for (int num6 = 0; num6 < 200; num6++)
                    //	{
                    //		Main.npc[num6] = new NPC();
                    //	}
                    //	for (int num7 = 0; num7 < 1000; num7++)
                    //	{
                    //		Main.projectile[num7] = new Projectile();
                    //	}
                    //	for (int num8 = 0; num8 < 8000; num8++)
                    //	{
                    //		Main.chest[num8] = null;
                    //	}
                    //	for (int num9 = 0; num9 < 1000; num9++)
                    //	{
                    //		Main.sign[num9] = null;
                    //	}
                    //	for (int num10 = 0; num10 < Liquid.maxLiquid; num10++)
                    //	{
                    //		Main.liquid[num10] = new Liquid();
                    //	}
                    //	for (int num11 = 0; num11 < 50000; num11++)
                    //	{
                    //		Main.liquidBuffer[num11] = new LiquidBuffer();
                    //	}
                    //	//setWorldSize();
                    //	Main.bottomWorld = Main.maxTilesY * 16;
                    //	Main.rightWorld = Main.maxTilesX * 16;
                    //	Main.maxSectionsX = Main.maxTilesX / 200;
                    //	Main.maxSectionsY = Main.maxTilesY / 150; 
                    //	Star.SpawnStars();
                    //	//worldCleared = true;
                    //	//WorldGen.clearWorld();
                    #endregion
                    if (versionNumber >= 209)
                    {
                        Main.GameMode = reader.ReadInt32();
                        if (versionNumber >= 222)
                        {
                            Main.drunkWorld = reader.ReadBoolean();
                        }
                        if (versionNumber >= 227)
                        {
                            Main.getGoodWorld = reader.ReadBoolean();
                        }
                        if (versionNumber >= 238)
                        {
                            Main.tenthAnniversaryWorld = reader.ReadBoolean();
                        }
                        if (versionNumber >= 239)
                        {
                            Main.dontStarveWorld = reader.ReadBoolean();
                        }
                        if (versionNumber >= 241)
                        {
                            Main.notTheBeesWorld = reader.ReadBoolean();
                        }
                    }
                    else
                    {
                        if (versionNumber >= 112)
                        {
                            Main.GameMode = (reader.ReadBoolean() ? 1 : 0);
                        }
                        else
                        {
                            Main.GameMode = 0;
                        }
                        if (versionNumber == 208 && reader.ReadBoolean())
                        {
                            Main.GameMode = 2;
                        }
                    }
                    if (versionNumber >= 141)
                    {
                        Main.ActiveWorldFileData.CreationTime = DateTime.FromBinary(reader.ReadInt64());
                    }
                    Main.moonType = reader.ReadByte();
                    Main.treeX[0] = reader.ReadInt32();
                    Main.treeX[1] = reader.ReadInt32();
                    Main.treeX[2] = reader.ReadInt32();
                    Main.treeStyle[0] = reader.ReadInt32();
                    Main.treeStyle[1] = reader.ReadInt32();
                    Main.treeStyle[2] = reader.ReadInt32();
                    Main.treeStyle[3] = reader.ReadInt32();
                    Main.caveBackX[0] = reader.ReadInt32();
                    Main.caveBackX[1] = reader.ReadInt32();
                    Main.caveBackX[2] = reader.ReadInt32();
                    Main.caveBackStyle[0] = reader.ReadInt32();
                    Main.caveBackStyle[1] = reader.ReadInt32();
                    Main.caveBackStyle[2] = reader.ReadInt32();
                    Main.caveBackStyle[3] = reader.ReadInt32();
                    Main.iceBackStyle = reader.ReadInt32();
                    Main.jungleBackStyle = reader.ReadInt32();
                    Main.hellBackStyle = reader.ReadInt32();
                    Main.spawnTileX = reader.ReadInt32();
                    Main.spawnTileY = reader.ReadInt32();
                    Main.worldSurface = reader.ReadDouble();
                    Main.rockLayer = reader.ReadDouble();
                    WorldFile._tempTime = reader.ReadDouble();
                    WorldFile._tempDayTime = reader.ReadBoolean();
                    WorldFile._tempMoonPhase = reader.ReadInt32();
                    WorldFile._tempBloodMoon = reader.ReadBoolean();
                    WorldFile._tempEclipse = reader.ReadBoolean();
                    Main.eclipse = WorldFile._tempEclipse;
                    Main.dungeonX = reader.ReadInt32();
                    Main.dungeonY = reader.ReadInt32();
                    WorldGen.crimson = reader.ReadBoolean();
                    NPC.downedBoss1 = reader.ReadBoolean();
                    NPC.downedBoss2 = reader.ReadBoolean();
                    NPC.downedBoss3 = reader.ReadBoolean();
                    NPC.downedQueenBee = reader.ReadBoolean();
                    NPC.downedMechBoss1 = reader.ReadBoolean();
                    NPC.downedMechBoss2 = reader.ReadBoolean();
                    NPC.downedMechBoss3 = reader.ReadBoolean();
                    NPC.downedMechBossAny = reader.ReadBoolean();
                    NPC.downedPlantBoss = reader.ReadBoolean();
                    NPC.downedGolemBoss = reader.ReadBoolean();
                    if (versionNumber >= 118)
                    {
                        NPC.downedSlimeKing = reader.ReadBoolean();
                    }
                    NPC.savedGoblin = reader.ReadBoolean();
                    NPC.savedWizard = reader.ReadBoolean();
                    NPC.savedMech = reader.ReadBoolean();
                    NPC.downedGoblins = reader.ReadBoolean();
                    NPC.downedClown = reader.ReadBoolean();
                    NPC.downedFrost = reader.ReadBoolean();
                    NPC.downedPirates = reader.ReadBoolean();
                    WorldGen.shadowOrbSmashed = reader.ReadBoolean();
                    WorldGen.spawnMeteor = reader.ReadBoolean();
                    WorldGen.shadowOrbCount = reader.ReadByte();
                    WorldGen.altarCount = reader.ReadInt32();
                    Main.hardMode = reader.ReadBoolean();
                    Main.invasionDelay = reader.ReadInt32();
                    Main.invasionSize = reader.ReadInt32();
                    Main.invasionType = reader.ReadInt32();
                    Main.invasionX = reader.ReadDouble();
                    if (versionNumber >= 118)
                    {
                        Main.slimeRainTime = reader.ReadDouble();
                    }
                    if (versionNumber >= 113)
                    {
                        Main.sundialCooldown = reader.ReadByte();
                    }
                    WorldFile._tempRaining = reader.ReadBoolean();
                    WorldFile._tempRainTime = reader.ReadInt32();
                    WorldFile._tempMaxRain = reader.ReadSingle();
                    WorldGen.SavedOreTiers.Cobalt = reader.ReadInt32();
                    WorldGen.SavedOreTiers.Mythril = reader.ReadInt32();
                    WorldGen.SavedOreTiers.Adamantite = reader.ReadInt32();
                    WorldGen.setBG(0, reader.ReadByte());
                    WorldGen.setBG(1, reader.ReadByte());
                    WorldGen.setBG(2, reader.ReadByte());
                    WorldGen.setBG(3, reader.ReadByte());
                    WorldGen.setBG(4, reader.ReadByte());
                    WorldGen.setBG(5, reader.ReadByte());
                    WorldGen.setBG(6, reader.ReadByte());
                    WorldGen.setBG(7, reader.ReadByte());
                    Main.cloudBGActive = reader.ReadInt32();
                    Main.cloudBGAlpha = (((double)Main.cloudBGActive < 1.0) ? 0f : 1f);
                    Main.cloudBGActive = -WorldGen.genRand.Next(8640, 86400);
                    Main.numClouds = reader.ReadInt16();
                    Main.windSpeedTarget = reader.ReadSingle();
                    Main.windSpeedCurrent = Main.windSpeedTarget;
                    if (versionNumber < 95)
                    {
                        goto LOADHEADER_END;
                    }
                    Main.anglerWhoFinishedToday.Clear();
                    for (int anglerWhoFinishedTodayNum = reader.ReadInt32(); anglerWhoFinishedTodayNum > 0; anglerWhoFinishedTodayNum--)
                    {
                        Main.anglerWhoFinishedToday.Add(reader.ReadString());
                    }
                    if (versionNumber < 99)
                    {
                        goto LOADHEADER_END;
                    }
                    NPC.savedAngler = reader.ReadBoolean();
                    if (versionNumber < 101)
                    {
                        goto LOADHEADER_END;
                    }
                    Main.anglerQuest = reader.ReadInt32();
                    if (versionNumber < 104)
                    {
                        goto LOADHEADER_END;
                    }
                    NPC.savedStylist = reader.ReadBoolean();
                    if (versionNumber >= 129)
                    {
                        NPC.savedTaxCollector = reader.ReadBoolean();
                    }
                    if (versionNumber >= 201)
                    {
                        NPC.savedGolfer = reader.ReadBoolean();
                    }
                    if (versionNumber < 107)
                    {
                        if (Main.invasionType > 0 && Main.invasionSize > 0)
                        {
                            Main.FakeLoadInvasionStart();
                        }
                    }
                    else
                    {
                        Main.invasionSizeStart = reader.ReadInt32();
                    }
                    if (versionNumber < 108)
                    {
                        WorldFile._tempCultistDelay = 86400;
                    }
                    else
                    {
                        WorldFile._tempCultistDelay = reader.ReadInt32();
                    }
                    if (versionNumber < 109)
                    {
                        goto LOADHEADER_END;
                    }
                    int LoadHeadernum2 = reader.ReadInt16();
                    for (int i = 0; i < LoadHeadernum2; i++)
                    {
                        if (i < 670)
                        {
                            NPC.killCount[i] = reader.ReadInt32();
                        }
                        else
                        {
                            reader.ReadInt32();
                        }
                    }
                    if (versionNumber < 128)
                    {
                        goto LOADHEADER_END;
                    }
                    Main.fastForwardTime = reader.ReadBoolean();
                    Main.UpdateTimeRate();
                    if (versionNumber < 131)
                    {
                        goto LOADHEADER_END;
                    }
                    NPC.downedFishron = reader.ReadBoolean();
                    NPC.downedMartians = reader.ReadBoolean();
                    NPC.downedAncientCultist = reader.ReadBoolean();
                    NPC.downedMoonlord = reader.ReadBoolean();
                    NPC.downedHalloweenKing = reader.ReadBoolean();
                    NPC.downedHalloweenTree = reader.ReadBoolean();
                    NPC.downedChristmasIceQueen = reader.ReadBoolean();
                    NPC.downedChristmasSantank = reader.ReadBoolean();
                    NPC.downedChristmasTree = reader.ReadBoolean();
                    if (versionNumber < 140)
                    {
                        goto LOADHEADER_END;
                    }
                    NPC.downedTowerSolar = reader.ReadBoolean();
                    NPC.downedTowerVortex = reader.ReadBoolean();
                    NPC.downedTowerNebula = reader.ReadBoolean();
                    NPC.downedTowerStardust = reader.ReadBoolean();
                    NPC.TowerActiveSolar = reader.ReadBoolean();
                    NPC.TowerActiveVortex = reader.ReadBoolean();
                    NPC.TowerActiveNebula = reader.ReadBoolean();
                    NPC.TowerActiveStardust = reader.ReadBoolean();
                    NPC.LunarApocalypseIsUp = reader.ReadBoolean();
                    if (NPC.TowerActiveSolar)
                    {
                        NPC.ShieldStrengthTowerSolar = NPC.ShieldStrengthTowerMax;
                    }
                    if (NPC.TowerActiveVortex)
                    {
                        NPC.ShieldStrengthTowerVortex = NPC.ShieldStrengthTowerMax;
                    }
                    if (NPC.TowerActiveNebula)
                    {
                        NPC.ShieldStrengthTowerNebula = NPC.ShieldStrengthTowerMax;
                    }
                    if (NPC.TowerActiveStardust)
                    {
                        NPC.ShieldStrengthTowerStardust = NPC.ShieldStrengthTowerMax;
                    }
                    if (versionNumber < 170)
                    {
                        WorldFile._tempPartyManual = false;
                        WorldFile._tempPartyGenuine = false;
                        WorldFile._tempPartyCooldown = 0;
                        WorldFile.TempPartyCelebratingNPCs.Clear();
                    }
                    else
                    {
                        WorldFile._tempPartyManual = reader.ReadBoolean();
                        WorldFile._tempPartyGenuine = reader.ReadBoolean();
                        WorldFile._tempPartyCooldown = reader.ReadInt32();
                        int LoadHeadernum3 = reader.ReadInt32();
                        WorldFile.TempPartyCelebratingNPCs.Clear();
                        for (int j = 0; j < LoadHeadernum3; j++)
                        {
                            WorldFile.TempPartyCelebratingNPCs.Add(reader.ReadInt32());
                        }
                    }
                    if (versionNumber < 174)
                    {
                        WorldFile._tempSandstormHappening = false;
                        WorldFile._tempSandstormTimeLeft = 0;
                        WorldFile._tempSandstormSeverity = 0f;
                        WorldFile._tempSandstormIntendedSeverity = 0f;
                    }
                    else
                    {
                        WorldFile._tempSandstormHappening = reader.ReadBoolean();
                        WorldFile._tempSandstormTimeLeft = reader.ReadInt32();
                        WorldFile._tempSandstormSeverity = reader.ReadSingle();
                        WorldFile._tempSandstormIntendedSeverity = reader.ReadSingle();
                    }
                    #region DD2Event.Load(reader, versionNumber);
                    if (versionNumber < 178)
                    {
                        NPC.savedBartender = false;
                        DD2Event.ResetProgressEntirely();
                    }
                    NPC.savedBartender = reader.ReadBoolean();
                    DD2Event.DownedInvasionT1 = reader.ReadBoolean();
                    DD2Event.DownedInvasionT2 = reader.ReadBoolean();
                    DD2Event.DownedInvasionT3 = reader.ReadBoolean();
                    #endregion
                    if (versionNumber > 194)
                    {
                        WorldGen.setBG(8, reader.ReadByte());
                    }
                    else
                    {
                        WorldGen.setBG(8, 0);
                    }
                    if (versionNumber >= 215)
                    {
                        WorldGen.setBG(9, reader.ReadByte());
                    }
                    else
                    {
                        WorldGen.setBG(9, 0);
                    }
                    if (versionNumber > 195)
                    {
                        WorldGen.setBG(10, reader.ReadByte());
                        WorldGen.setBG(11, reader.ReadByte());
                        WorldGen.setBG(12, reader.ReadByte());
                    }
                    else
                    {
                        WorldGen.setBG(10, WorldGen.treeBG1);
                        WorldGen.setBG(11, WorldGen.treeBG1);
                        WorldGen.setBG(12, WorldGen.treeBG1);
                    }
                    if (versionNumber >= 204)
                    {
                        NPC.combatBookWasUsed = reader.ReadBoolean();
                    }
                    if (versionNumber < 207)
                    {
                        WorldFile._tempLanternNightCooldown = 0;
                        WorldFile._tempLanternNightGenuine = false;
                        WorldFile._tempLanternNightManual = false;
                        WorldFile._tempLanternNightNextNightIsGenuine = false;
                    }
                    else
                    {
                        WorldFile._tempLanternNightCooldown = reader.ReadInt32();
                        WorldFile._tempLanternNightGenuine = reader.ReadBoolean();
                        WorldFile._tempLanternNightManual = reader.ReadBoolean();
                        WorldFile._tempLanternNightNextNightIsGenuine = reader.ReadBoolean();
                    }
                    #region WorldGen.TreeTops.Load(reader, versionNumber);
                    if (versionNumber < 211)
                    {
                        WorldGen.TreeTops.CopyExistingWorldInfo();
                    }
                    int num = reader.ReadInt32();
                    for (int i = 0; i < num && i < WorldGen.TreeTops._variations.Length; i++)
                    {
                        WorldGen.TreeTops._variations[i] = reader.ReadInt32();
                    }
                    #endregion
                    if (versionNumber >= 212)
                    {
                        Main.forceHalloweenForToday = reader.ReadBoolean();
                        Main.forceXMasForToday = reader.ReadBoolean();
                    }
                    else
                    {
                        Main.forceHalloweenForToday = false;
                        Main.forceXMasForToday = false;
                    }
                    if (versionNumber >= 216)
                    {
                        WorldGen.SavedOreTiers.Copper = reader.ReadInt32();
                        WorldGen.SavedOreTiers.Iron = reader.ReadInt32();
                        WorldGen.SavedOreTiers.Silver = reader.ReadInt32();
                        WorldGen.SavedOreTiers.Gold = reader.ReadInt32();
                    }
                    else
                    {
                        WorldGen.SavedOreTiers.Copper = -1;
                        WorldGen.SavedOreTiers.Iron = -1;
                        WorldGen.SavedOreTiers.Silver = -1;
                        WorldGen.SavedOreTiers.Gold = -1;
                    }
                    if (versionNumber >= 217)
                    {
                        NPC.boughtCat = reader.ReadBoolean();
                        NPC.boughtDog = reader.ReadBoolean();
                        NPC.boughtBunny = reader.ReadBoolean();
                    }
                    else
                    {
                        NPC.boughtCat = false;
                        NPC.boughtDog = false;
                        NPC.boughtBunny = false;
                    }
                    if (versionNumber >= 223)
                    {
                        NPC.downedEmpressOfLight = reader.ReadBoolean();
                        NPC.downedQueenSlime = reader.ReadBoolean();
                    }
                    else
                    {
                        NPC.downedEmpressOfLight = false;
                        NPC.downedQueenSlime = false;
                    }
                    if (versionNumber >= 240)
                    {
                        NPC.downedDeerclops = reader.ReadBoolean();
                    }
                    else
                    {
                        NPC.downedDeerclops = false;
                    }
                    #endregion
                    debugPointerString = "After LoadHeader";

                LOADHEADER_END:
                    // ======================
                    CreateCustomTileProvider();
                    // ======================

                    StructTile[,] providerData = FakeProviderAPI.World.Data;
                    //TODO: make .Data a StructTile[] to not have to do this ugly conversion
                    var length = providerData.GetLength(0) * providerData.GetLength(1);
                    Span<StructTile> tiles = new Span<StructTile>();
                    fixed (StructTile* p = providerData)
                    {
                        tiles = new Span<StructTile>(p, length);
                    }

                    /*if (reader.BaseStream.Position != positions[1])
					{
						loadWorldRet = 5;
                        goto WORLDLOAD_END;
					}*/
                    #region LoadWorldTiles 
                    for (int tileX = 0; tileX < Main.maxTilesX; tileX++)
                    {
                        for (int tileY = 0; tileY < Main.maxTilesY; tileY++)
                        {
                            //tiles[Index(tileX, tileY)] = new StructTile();
                            int type = -1;
                            byte b;
                            byte b2 = (b = 0);
                            //StructTile tiles[Index(tileX, tileY)] = tiles[Index(tileX, tileY)];
                            byte b3 = reader.ReadByte();
                            if ((b3 & 1) == 1)
                            {
                                b2 = reader.ReadByte();
                                if ((b2 & 1) == 1)
                                {
                                    b = reader.ReadByte();
                                }
                            }
                            byte b4;
                            if ((b3 & 2) == 2)
                            {
                                tiles[Index(tileX, tileY)].active(true);
                                if ((b3 & 0x20) == 32)
                                {
                                    b4 = reader.ReadByte();
                                    type = reader.ReadByte();
                                    type = (type << 8) | b4;
                                }
                                else
                                {
                                    type = reader.ReadByte();
                                }
                                tiles[Index(tileX, tileY)].type = (ushort)type;
                                if (importance[type])
                                {
                                    tiles[Index(tileX, tileY)].frameX = reader.ReadInt16();
                                    tiles[Index(tileX, tileY)].frameY = reader.ReadInt16();
                                    if (tiles[Index(tileX, tileY)].type == 144)
                                    {
                                        tiles[Index(tileX, tileY)].frameY = 0;
                                    }
                                }
                                else
                                {
                                    tiles[Index(tileX, tileY)].frameX = -1;
                                    tiles[Index(tileX, tileY)].frameY = -1;
                                }
                                if ((b & 8) == 8)
                                {
                                    tiles[Index(tileX, tileY)].color(reader.ReadByte());
                                }
                            }
                            if ((b3 & 4) == 4)
                            {
                                tiles[Index(tileX, tileY)].wall = reader.ReadByte();
                                if (tiles[Index(tileX, tileY)].wall >= 316)
                                {
                                    tiles[Index(tileX, tileY)].wall = 0;
                                }
                                if ((b & 0x10) == 16)
                                {
                                    tiles[Index(tileX, tileY)].wallColor(reader.ReadByte());
                                }
                            }
                            b4 = (byte)((b3 & 0x18) >> 3);
                            if (b4 != 0)
                            {
                                tiles[Index(tileX, tileY)].liquid = reader.ReadByte();
                                if (b4 > 1)
                                {
                                    if (b4 == 2)
                                    {
                                        tiles[Index(tileX, tileY)].lava(lava: true);
                                    }
                                    else
                                    {
                                        tiles[Index(tileX, tileY)].honey(honey: true);
                                    }
                                }
                            }
                            if (b2 > 1)
                            {
                                if ((b2 & 2) == 2)
                                {
                                    tiles[Index(tileX, tileY)].wire(wire: true);
                                }
                                if ((b2 & 4) == 4)
                                {
                                    tiles[Index(tileX, tileY)].wire2(wire2: true);
                                }
                                if ((b2 & 8) == 8)
                                {
                                    tiles[Index(tileX, tileY)].wire3(wire3: true);
                                }
                                b4 = (byte)((b2 & 0x70) >> 4);
                                if (b4 != 0 && (Main.tileSolid[tiles[Index(tileX, tileY)].type] || TileID.Sets.NonSolidSaveSlopes[tiles[Index(tileX, tileY)].type]))
                                {
                                    if (b4 == 1)
                                    {
                                        tiles[Index(tileX, tileY)].halfBrick(halfBrick: true);
                                    }
                                    else
                                    {
                                        tiles[Index(tileX, tileY)].slope((byte)(b4 - 1));
                                    }
                                }
                            }
                            if (b > 0)
                            {
                                if ((b & 2) == 2)
                                {
                                    tiles[Index(tileX, tileY)].actuator(actuator: true);
                                }
                                if ((b & 4) == 4)
                                {
                                    tiles[Index(tileX, tileY)].inActive(inActive: true);
                                }
                                if ((b & 0x20) == 32)
                                {
                                    tiles[Index(tileX, tileY)].wire4(wire4: true);
                                }
                                if ((b & 0x40) == 64)
                                {
                                    b4 = reader.ReadByte();
                                    tiles[Index(tileX, tileY)].wall = (ushort)((b4 << 8) | tiles[Index(tileX, tileY)].wall);
                                    if (tiles[Index(tileX, tileY)].wall >= 316)
                                    {
                                        tiles[Index(tileX, tileY)].wall = 0;
                                    }
                                }
                            }
                            int surfaceOffset = (byte)((b3 & 0xC0) >> 6) switch
                            {
                                0 => 0,
                                1 => reader.ReadByte(),
                                _ => reader.ReadInt16(),
                            };
                            if (type != -1)
                            {
                                if ((double)tileY <= Main.worldSurface)
                                {
                                    if ((double)(tileY + surfaceOffset) <= Main.worldSurface)
                                    {
                                        WorldGen.tileCounts[type] += (surfaceOffset + 1) * 5;
                                    }
                                    else
                                    {
                                        //Todo fix names
                                        int surfaceSubtract = (int)(Main.worldSurface - (double)tileY + 1.0);
                                        int tileCountAdd = surfaceOffset + 1 - surfaceSubtract;
                                        WorldGen.tileCounts[type] += surfaceSubtract * 5 + tileCountAdd;
                                    }
                                }
                                else
                                {
                                    WorldGen.tileCounts[type] += surfaceOffset + 1;
                                }
                            }
                            StructTile tile = tiles[Index(tileX, tileY)];
                            while (surfaceOffset > 0)
                            {
                                tileY++;
                                tiles[Index(tileX, tileY)] = tile;
                                surfaceOffset--;
                            }
                        }
                    }
                    WorldGen.AddUpAlignmentCounts(clearCounts: true);
                    if (versionNumber < 105)
                    {
                        WorldGen.FixHearts();
                    }
                    #endregion
                    debugPointerString = "After LoadWorldTiles";

                LOADWORLDTILES_END:
                    /*if (reader.BaseStream.Position != positions[2])
					{
						loadWorldRet = 5;
                        goto WORLDLOAD_END;
					}*/
                    #region comment

                    #region LoadChests
                    int chestnum1 = reader.ReadInt16();
                    int chestnum2 = reader.ReadInt16();
                    int chestnum3;
                    int chestnum4;
                    if (chestnum2 < 40)
                    {
                        chestnum3 = chestnum2;
                        chestnum4 = 0;
                    }
                    else
                    {
                        chestnum3 = 40;
                        chestnum4 = chestnum2 - 40;
                    }
                    int ichest;
                    for (ichest = 0; ichest < chestnum1; ichest++)
                    {
                        Chest chest = new Chest();
                        chest.x = reader.ReadInt32();
                        chest.y = reader.ReadInt32();
                        chest.name = reader.ReadString();
                        for (int jchest = 0; jchest < chestnum3; jchest++)
                        {
                            short chestnum5 = reader.ReadInt16();
                            Item item = new Item();
                            if (chestnum5 > 0)
                            {
                                item.netDefaults(reader.ReadInt32());
                                item.stack = chestnum5;
                                item.Prefix(reader.ReadByte());
                            }
                            else if (chestnum5 < 0)
                            {
                                item.netDefaults(reader.ReadInt32());
                                item.Prefix(reader.ReadByte());
                                item.stack = 1;
                            }
                            chest.item[jchest] = item;
                        }
                        for (int k = 0; k < chestnum4; k++)
                        {
                            short num5 = reader.ReadInt16();
                            if (num5 > 0)
                            {
                                reader.ReadInt32();
                                reader.ReadByte();
                            }
                        }
                        Main.chest[ichest] = chest;
                    }
                    List<Point16> list = new List<Point16>();
                    for (int chestIndex = 0; chestIndex < ichest; chestIndex++)
                    {
                        if (Main.chest[chestIndex] != null)
                        {
                            Point16 item2 = new Point16(Main.chest[chestIndex].x, Main.chest[chestIndex].y);
                            if (list.Contains(item2))
                            {
                                Main.chest[chestIndex] = null;
                            }
                            else
                            {
                                list.Add(item2);
                            }
                        }
                    }
                    for (; ichest < 8000; ichest++)
                    {
                        Main.chest[ichest] = null;
                    }
                    if (versionNumber < 115)
                    {
                        //FixDresserChests();
                        for (int chestX = 0; ichest < Main.maxTilesX; chestX++)
                        {
                            for (int chestY = 0; chestY < Main.maxTilesY; chestY++)
                            {
                                StructTile tile = tiles[Index(chestX, chestY)];
                                if (tile.active() && tile.type == 88 && tile.frameX % 54 == 0 && tile.frameY % 36 == 0)
                                {
                                    Chest.CreateChest(chestX, chestY);
                                }
                            }
                        }
                    }
                    #endregion
                    debugPointerString = "After LoadChests";
                LOADCHESTS_END:
                    /*if (reader.BaseStream.Position != positions[3])
                    {
                        loadWorldRet = 5;
                        goto WORLDLOAD_END;
                    }*/
                    #region LoadSigns
                    short signCount = reader.ReadInt16();
                    int signIndex;
                    for (signIndex = 0; signIndex < signCount; signIndex++)
                    {
                        string text = reader.ReadString();
                        int signX = reader.ReadInt32();
                        int signY = reader.ReadInt32();
                        StructTile tile = tiles[Index(signX, signY)];
                        Sign sign;
                        if (tile.active() && Main.tileSign[tile.type])
                        {
                            sign = new Sign();
                            sign.text = text;
                            sign.x = signX;
                            sign.y = signY;
                        }
                        else
                        {
                            sign = null;
                        }
                        Main.sign[signIndex] = sign;
                    }
                    List<Point16> signPositions = new List<Point16>();
                    for (int i = 0; i < 1000; i++)
                    {
                        if (Main.sign[i] != null)
                        {
                            Point16 signPos = new Point16(Main.sign[i].x, Main.sign[i].y);
                            if (signPositions.Contains(signPos))
                            {
                                Main.sign[i] = null;
                            }
                            else
                            {
                                signPositions.Add(signPos);
                            }
                        }
                    }
                    for (; signIndex < 1000; signIndex++)
                    {
                        Main.sign[signIndex] = null;
                    }
                    #endregion
                    debugPointerString = "After LoadSigns";

                LOADSIGNS_END:
                    /*  if (reader.BaseStream.Position != positions[4])
					  {
						  loadWorldRet = 5;
						  goto WORLDLOAD_END;
					  }*/
                    LoadNPCs(reader);
                    debugPointerString = "After LoadNPCs";

                    /*if (reader.BaseStream.Position != positions[5])
                    {
                        loadWorldRet = 5;
                        goto WORLDLOAD_END;
                    }*/
                    if (versionNumber >= 116)
                    {
                        if (versionNumber < 122)
                        {
                            LoadDummies(reader);
                            /*if (reader.BaseStream.Position != positions[6])
                            {
                                loadWorldRet = 5;
                                goto WORLDLOAD_END;
                            }*/
                        }
                        else
                        {
                            LoadTileEntities(reader);
                            /* if (reader.BaseStream.Position != positions[6])
							 {
								 loadWorldRet = 5;
								 goto WORLDLOAD_END;
							 }*/
                        }
                    }
                    if (versionNumber >= 170)
                    {
                        LoadWeightedPressurePlates(reader);
                        /*if (reader.BaseStream.Position != positions[7])
						 {
							 loadWorldRet = 5;
							 goto WORLDLOAD_END;
						 }*/
                    }
                    if (versionNumber >= 189)
                    {
                        LoadTownManager(reader);
                        /* if (reader.BaseStream.Position != positions[8])
						 {
							 loadWorldRet = 5;
							 goto WORLDLOAD_END;
						 }*/
                    }
                    if (versionNumber >= 210)
                    {
                        LoadBestiary(reader, versionNumber);
                        /* if (reader.BaseStream.Position != positions[9])
						 {
							 loadWorldRet = 5;
							 goto WORLDLOAD_END;
						 }*/
                    }
                    else
                    {
                        LoadBestiaryForVersionsBefore210();
                    }
                    if (versionNumber >= 220)
                    {
                        LoadCreativePowers(reader, versionNumber);
                        /* if (reader.BaseStream.Position != positions[10])
						 {
							 loadWorldRet = 5;
							 goto WORLDLOAD_END;
						 }*/
                    }
                    loadWorldRet = LoadFooter(reader);
                    debugPointerString = "After LoadFooter";

                    #endregion
                    void LoadNPCs(UnsafeBinaryReader reader)
                    {
                        int num = 0;
                        bool flag = reader.ReadBoolean();
                        while (flag)
                        {
                            NPC nPC = Main.npc[num];
                            if (versionNumber >= 190)
                            {
                                nPC.SetDefaults(reader.ReadInt32());
                            }
                            else
                            {
                                nPC.SetDefaults(NPCID.FromLegacyName(reader.ReadString()));
                            }
                            nPC.GivenName = reader.ReadString();
                            nPC.position.X = reader.ReadSingle();
                            nPC.position.Y = reader.ReadSingle();
                            nPC.homeless = reader.ReadBoolean();
                            nPC.homeTileX = reader.ReadInt32();
                            nPC.homeTileY = reader.ReadInt32();
                            if (versionNumber >= 213 && ((BitsByte)reader.ReadByte())[0])
                            {
                                nPC.townNpcVariationIndex = reader.ReadInt32();
                            }
                            num++;
                            flag = reader.ReadBoolean();
                        }
                        if (versionNumber < 140)
                        {
                            return;
                        }
                        flag = reader.ReadBoolean();
                        while (flag)
                        {
                            NPC nPC = Main.npc[num];
                            if (versionNumber >= 190)
                            {
                                nPC.SetDefaults(reader.ReadInt32());
                            }
                            else
                            {
                                nPC.SetDefaults(NPCID.FromLegacyName(reader.ReadString()));
                            }
                            nPC.position = reader.ReadVector2();
                            num++;
                            flag = reader.ReadBoolean();
                        }
                    }
                    void LoadDummies(UnsafeBinaryReader reader)
                    {
                        /*
						int num = reader.ReadInt32();
						for (int i = 0; i < num; i++)
						{
							DeprecatedClassLeftInForLoading.dummies[i] = new DeprecatedClassLeftInForLoading(reader.ReadInt16(), reader.ReadInt16());
						}
						for (int j = num; j < 1000; j++)
						{
							DeprecatedClassLeftInForLoading.dummies[j] = null;
						}
						*/ // Removed from the game (Check OTAPI WorldFile.LoadDummies (1.4.3.6 and 1.4.3.2)).

                        int num = reader.ReadInt32();
                        for (int i = 0; i < num; i++)
                        {
                            reader.ReadInt16();
                            reader.ReadInt16();
                        }
                    }
                    void LoadTileEntities(UnsafeBinaryReader reader)
                    {
                        TileEntity.ByID.Clear();
                        TileEntity.ByPosition.Clear();
                        int tileEntitynum = reader.ReadInt32();
                        int tileEntityID = 0;
                        for (int i = 0; i < tileEntitynum; i++)
                        {
                            //TileEntity tileEntity = TileEntity.Read(reader);
                            #region InnerTileEntityRead
                            byte id = reader.ReadByte();
                            TileEntity tileEntity = TileEntity.manager.GenerateInstance(id);
                            if (tileEntity is null) continue;
                            tileEntity.type = id;
                            //tileEntity.ReadInner(reader, networkSend);
                            bool networkSend = false;
                            if (!networkSend)
                            {
                                tileEntity.ID = reader.ReadInt32();
                            }
                            tileEntity.Position = new Point16(reader.ReadInt16(), reader.ReadInt16());
                            //tileEntity.ReadExtraData(reader, networkSend);
                            //Abstract whyy ahhh
                            //Console.WriteLine($"Reading tile ent {tileEntity.type} #{tileEntity.ID} is {TileEntity.manager._types[tileEntity.type].GetType().Name}  at: {tileEntity.Position.X},{tileEntity.Position.Y}");
                            switch (TileEntity.manager._types[tileEntity.type])
                            {
                                case TELogicSensor logicSensor:
                                    logicSensor = (tileEntity as TELogicSensor);
                                    if (!networkSend)
                                    {
                                        logicSensor.logicCheck = (TELogicSensor.LogicCheckType)reader.ReadByte();
                                        logicSensor.On = reader.ReadBoolean();
                                    }
                                    break;
                                case TEDisplayDoll displayDoll:
                                    displayDoll = (tileEntity as TEDisplayDoll);
                                    BitsByte setItemDisplayDoll = reader.ReadByte();
                                    BitsByte setDyeDisplayDoll = reader.ReadByte();
                                    for (int itemDoll = 0; itemDoll < 8; itemDoll++)
                                    {
                                        displayDoll._items[itemDoll] = new Item();
                                        Item item = displayDoll._items[itemDoll];
                                        if (setItemDisplayDoll[itemDoll])
                                        {
                                            item.netDefaults(reader.ReadInt16());
                                            item.Prefix(reader.ReadByte());
                                            item.stack = reader.ReadInt16();
                                        }
                                    }
                                    for (int dyeDoll = 0; dyeDoll < 8; dyeDoll++)
                                    {
                                        displayDoll._dyes[dyeDoll] = new Item();
                                        Item item2 = displayDoll._dyes[dyeDoll];
                                        if (setDyeDisplayDoll[dyeDoll])
                                        {
                                            item2.netDefaults(reader.ReadInt16());
                                            item2.Prefix(reader.ReadByte());
                                            item2.stack = reader.ReadInt16();
                                        }
                                    }
                                    break;
                                case TEHatRack hatRack:
                                    hatRack = (tileEntity as TEHatRack);
                                    BitsByte hatRackFlags = reader.ReadByte();
                                    for (int itemHatRack = 0; itemHatRack < 2; itemHatRack++)
                                    {
                                        hatRack._items[itemHatRack] = new Item();
                                        Item item = hatRack._items[itemHatRack];
                                        if (hatRackFlags[itemHatRack])
                                        {
                                            item.netDefaults(reader.ReadInt16());
                                            item.Prefix(reader.ReadByte());
                                            item.stack = reader.ReadInt16();
                                        }
                                    }
                                    for (int dyeHatRack = 0; dyeHatRack < 2; dyeHatRack++)
                                    {
                                        hatRack._dyes[dyeHatRack] = new Item();
                                        Item item2 = hatRack._dyes[dyeHatRack];
                                        if (hatRackFlags[dyeHatRack + 2])
                                        {
                                            item2.netDefaults(reader.ReadInt16());
                                            item2.Prefix(reader.ReadByte());
                                            item2.stack = reader.ReadInt16();
                                        }
                                    }
                                    break;
                                case TEFoodPlatter foodPlatter:
                                    foodPlatter = (tileEntity as TEFoodPlatter);
                                    foodPlatter.item = new Item();
                                    foodPlatter.item.netDefaults(reader.ReadInt16());
                                    foodPlatter.item.Prefix(reader.ReadByte());
                                    foodPlatter.item.stack = reader.ReadInt16();
                                    break;
                                case TEWeaponsRack weaponRack:
                                    weaponRack = (tileEntity as TEWeaponsRack);
                                    weaponRack.item = new Item();
                                    weaponRack.item.netDefaults(reader.ReadInt16());
                                    weaponRack.item.Prefix(reader.ReadByte());
                                    weaponRack.item.stack = reader.ReadInt16();
                                    break;
                                case TEItemFrame itemFrame:
                                    itemFrame = (tileEntity as TEItemFrame);
                                    itemFrame.item = new Item();
                                    itemFrame.item.netDefaults(reader.ReadInt16());
                                    itemFrame.item.Prefix(reader.ReadByte());
                                    itemFrame.item.stack = reader.ReadInt16();
                                    break;
                                case TETrainingDummy trainingDummy:
                                    trainingDummy = (tileEntity as TETrainingDummy);
                                    trainingDummy.npc = reader.ReadInt16();
                                    break;
                            }
                            #endregion
                            tileEntity.ID = tileEntityID++;
                            TileEntity.ByID[tileEntity.ID] = tileEntity;
                            if (TileEntity.ByPosition.TryGetValue(tileEntity.Position, out var value))
                            {
                                TileEntity.ByID.Remove(value.ID);
                            }
                            TileEntity.ByPosition[tileEntity.Position] = tileEntity;
                        }
                        TileEntity.TileEntitiesNextID = tileEntitynum;
                        List<Point16> list = new List<Point16>();
                        foreach (KeyValuePair<Point16, TileEntity> item in TileEntity.ByPosition)
                        {
                            if (!WorldGen.InWorld(item.Value.Position.X, item.Value.Position.Y, 1))
                            {
                                list.Add(item.Value.Position);
                            }
                            else if (!TileEntity.manager.CheckValidTile(item.Value.type, item.Value.Position.X, item.Value.Position.Y))
                            {
                                list.Add(item.Value.Position);
                            }
                        }
                        try
                        {
                            foreach (Point16 item2 in list)
                            {
                                TileEntity tileEntity2 = TileEntity.ByPosition[item2];
                                if (TileEntity.ByID.ContainsKey(tileEntity2.ID))
                                {
                                    TileEntity.ByID.Remove(tileEntity2.ID);
                                }
                                if (TileEntity.ByPosition.ContainsKey(item2))
                                {
                                    TileEntity.ByPosition.Remove(item2);
                                }
                            }
                        }
                        catch
                        {
                        }
                    }
                    void LoadWeightedPressurePlates(UnsafeBinaryReader reader)
                    {
                        PressurePlateHelper.Reset();
                        PressurePlateHelper.NeedsFirstUpdate = true;
                        int pressurePlateNum = reader.ReadInt32();
                        for (int i = 0; i < pressurePlateNum; i++)
                        {
                            Point key = new Point(reader.ReadInt32(), reader.ReadInt32());
                            PressurePlateHelper.PressurePlatesPressed.Add(key, new bool[255]);
                        }
                    }
                    void LoadTownManager(UnsafeBinaryReader reader)
                    {
                        //WorldGen.TownManager.Load(reader);
                        //Clear();
                        WorldGen.TownManager._roomLocationPairs.Clear();
                        for (int i = 0; i < WorldGen.TownManager._hasRoom.Length; i++)
                        {
                            WorldGen.TownManager._hasRoom[i] = false;
                        }
                        int num = reader.ReadInt32();
                        for (int i = 0; i < num; i++)
                        {
                            int num2 = reader.ReadInt32();
                            Point item = new Point(reader.ReadInt32(), reader.ReadInt32());
                            WorldGen.TownManager._roomLocationPairs.Add(Tuple.Create(num2, item));
                            WorldGen.TownManager._hasRoom[num2] = true;
                        }
                    }
                    void LoadBestiary(UnsafeBinaryReader reader, int loadVersionNumber)
                    {
                        //Main.BestiaryTracker.Load(reader, loadVersionNumber);
                        //Kills.Load(reader, gameVersionSaveWasMadeOn);
                        int killsCount = reader.ReadInt32();
                        for (int i = 0; i < killsCount; i++)
                        {
                            string key = reader.ReadString();
                            int value = reader.ReadInt32();
                            Main.BestiaryTracker.Kills._killCountsByNpcId[key] = value;
                        }
                        //Sights.Load(reader, gameVersionSaveWasMadeOn);
                        int sightsCount = reader.ReadInt32();
                        for (int i = 0; i < sightsCount; i++)
                        {
                            string item = reader.ReadString();
                            Main.BestiaryTracker.Sights._wasNearPlayer.Add(item);
                        }
                        //Chats.Load(reader, gameVersionSaveWasMadeOn);
                        int chatCount = reader.ReadInt32();
                        for (int i = 0; i < chatCount; i++)
                        {
                            string item = reader.ReadString();
                            Main.BestiaryTracker.Chats._chattedWithPlayer.Add(item);
                        }
                    }
                    void LoadBestiaryForVersionsBefore210()
                    {
                        Main.BestiaryTracker.FillBasedOnVersionBefore210();
                    }
                    void LoadCreativePowers(UnsafeBinaryReader reader, int loadVersionNumber)
                    {
                        //CreativePowerManager.Instance.LoadFromWorld(reader, loadVersionNumber);
                        while (reader.ReadBoolean())
                        {
                            ushort key = reader.ReadUInt16();
                            if (CreativePowerManager.Instance._powersById.TryGetValue(key, out var value))
                            {
                                IPersistentPerWorldContent persistentPerWorldContent = value as IPersistentPerWorldContent;
                                if (persistentPerWorldContent != null)
                                {
                                    //persistentPerWorldContent.Load(reader, loadVersionNumber);
                                    //Sigh why an interface...
                                    switch (persistentPerWorldContent)
                                    {
                                        case ModifyTimeRate modifyTimeRatePower:
                                            modifyTimeRatePower._sliderCurrentValueCache = reader.ReadSingle();
                                            modifyTimeRatePower.UpdateInfoFromSliderValueCache();
                                            break;
                                        case DifficultySliderPower difficultySliderPower:
                                            difficultySliderPower._sliderCurrentValueCache = reader.ReadSingle();
                                            difficultySliderPower.UpdateInfoFromSliderValueCache();
                                            break;
                                        case FreezeTime freezeTimePower:
                                            freezeTimePower.SetPowerInfo(reader.ReadBoolean());
                                            break;
                                        case FreezeWindDirectionAndStrength freezeWindDirectionAndStrenghtPower:
                                            freezeWindDirectionAndStrenghtPower.SetPowerInfo(reader.ReadBoolean());
                                            break;
                                        case FreezeRainPower freezeRainPower:
                                            freezeRainPower.SetPowerInfo(reader.ReadBoolean());
                                            break;
                                        case StopBiomeSpreadPower stopBiomeSpreadPower:
                                            stopBiomeSpreadPower.SetPowerInfo(reader.ReadBoolean());
                                            break;
                                        default:
                                            throw new InvalidDataException(
                                                $"Tried to load invalid creativepower type " +
                                                $"{persistentPerWorldContent.GetType()} " +
                                                $"for peristent content.");
                                    }
                                    continue;
                                }
                                break;
                            }
                            break;
                        }
                    }
                    int LoadFooter(UnsafeBinaryReader reader)
                    {
                        if (!reader.ReadBoolean())
                        {
                            return 6;
                        }
                        if (reader.ReadString() != Main.worldName)
                        {
                            return 6;
                        }
                        if (reader.ReadInt32() != Main.worldID)
                        {
                            return 6;
                        }
                        return 0;
                    }
                #endregion
                /*if (versionNumber < 141)
                {
                    if (!loadFromCloud)
                    {
                        Main.ActiveWorldFileData.CreationTime = File.GetCreationTime(Main.worldPathName);
                    }
                    else
                    {
                        Main.ActiveWorldFileData.CreationTime = DateTime.Now;
                    }
                }*/
                WORLDLOAD_END:
                    //Console.WriteLine($"left WORLDLOAD at {debugPointerString}");
                    #region CheckSavedOreTiers

                    //CheckSavedOreTiers();
                    //void CheckSavedOreTiers()
                    //{
                    //	if (WorldGen.SavedOreTiers.Copper != -1 && WorldGen.SavedOreTiers.Iron != -1 && WorldGen.SavedOreTiers.Silver != -1 && WorldGen.SavedOreTiers.Gold != -1)
                    //	{
                    //		return;
                    //	}
                    //	int[] array = WorldGen.CountTileTypesInWorld(7, 166, 6, 167, 9, 168, 8, 169);
                    //	for (int i = 0; i < array.Length; i += 2)
                    //	{
                    //		int num = array[i];
                    //		int num2 = array[i + 1];
                    //		switch (i)
                    //		{
                    //			case 0:
                    //				if (num > num2)
                    //				{
                    //					WorldGen.SavedOreTiers.Copper = 7;
                    //				}
                    //				else
                    //				{
                    //					WorldGen.SavedOreTiers.Copper = 166;
                    //				}
                    //				break;
                    //			case 2:
                    //				if (num > num2)
                    //				{
                    //					WorldGen.SavedOreTiers.Iron = 6;
                    //				}
                    //				else
                    //				{
                    //					WorldGen.SavedOreTiers.Iron = 167;
                    //				}
                    //				break;
                    //			case 4:
                    //				if (num > num2)
                    //				{
                    //					WorldGen.SavedOreTiers.Silver = 9;
                    //				}
                    //				else
                    //				{
                    //					WorldGen.SavedOreTiers.Silver = 168;
                    //				}
                    //				break;
                    //			case 6:
                    //				if (num > num2)
                    //				{
                    //					WorldGen.SavedOreTiers.Gold = 8;
                    //				}
                    //				else
                    //				{
                    //					WorldGen.SavedOreTiers.Gold = 169;
                    //				}
                    //				break;
                    //		}
                    //	}
                    //}				
                    #endregion
                    debugPointerString = "After CheckedSavedOreTiers";

                    reader.Close();
                    if (loadWorldRet != 0)
                    {
                        throw (new Exception("LoadWorldRet != 0"));
                        WorldGen.loadFailed = true;
                    }
                    else
                    {
                        WorldGen.loadSuccess = true;
                    }
                    if (WorldGen.loadFailed || !WorldGen.loadSuccess)
                    {
                        throw (new Exception("WORLD LOAD FAILED: " + debugPointerString));
                    }
                    #region ConvertOldTileEntities
                    //List<Point> tileEntitiesPoint1 = new List<Point>();
                    //List<Point> tileEntitiesPoint2 = new List<Point>();
                    //for (int i = 0; i < Main.maxTilesX; i++)
                    //{
                    //	for (int j = 0; j < Main.maxTilesY; j++)
                    //	{
                    //		StructTile tile = tiles[Index(i, j)];
                    //		if ((tile.type == 128 || tile.type == 269) && tile.frameY == 0 && (tile.frameX % 100 == 0 || tile.frameX % 100 == 36))
                    //		{
                    //			tileEntitiesPoint1.Add(new Point(i, j));
                    //		}
                    //		if (tile.type == 334 && tile.frameY == 0 && tile.frameX % 54 == 0)
                    //		{
                    //			tileEntitiesPoint2.Add(new Point(i, j));
                    //		}
                    //		if (tile.type == 49 && (tile.frameX == -1 || tile.frameY == -1))
                    //		{
                    //			tile.frameX = 0;
                    //			tile.frameY = 0;
                    //		}
                    //	}
                    //}
                    //foreach (Point point in tileEntitiesPoint1)
                    //{
                    //	if (!WorldGen.InWorld(point.X, point.Y, 5))
                    //	{
                    //		continue;
                    //	}
                    //	int frameX = tiles[Index(point.X, point.Y)].frameX;
                    //	int frameX2 = tiles[Index(point.X, point.Y+1)].frameX;
                    //	int frameX3 = tiles[Index(point.X, point.Y+2)].frameX;
                    //	for (int k = 0; k < 2; k++)
                    //	{
                    //		for (int l = 0; l < 3; l++)
                    //		{
                    //			StructTile tile2 = tiles[Index(point.X + k, point.Y + l)];
                    //			tile2.frameX %= 100;
                    //			if (tile2.type == 269)
                    //			{
                    //				tile2.frameX += 72;
                    //			}
                    //			tile2.type = 470;
                    //		}
                    //	}
                    //	int num = TEDisplayDoll.Place(point.X, point.Y);
                    //	if (num != -1)
                    //	{
                    //		(TileEntity.ByID[num] as TEDisplayDoll).SetInventoryFromMannequin(frameX, frameX2, frameX3);
                    //	}
                    //}
                    //foreach (Point point2 in tileEntitiesPoint2)
                    //{
                    //	if (!WorldGen.InWorld(point2.X, point2.Y, 5))
                    //	{
                    //		continue;
                    //	}
                    //	bool flag = tiles[Index(point2.X, point2.Y)].frameX >= 54;
                    //	short frameX4 = tiles[Index(point2.X, point2.Y + 1)].frameX;
                    //	int frameX5 = tiles[Index(point2.X + 1, point2.Y + 1)].frameX;
                    //	bool flag2 = frameX4 >= 5000;
                    //	int tileEntNum2 = frameX4 % 5000;
                    //	tileEntNum2 -= 100;
                    //	int prefix = frameX5 - ((frameX5 >= 25000) ? 25000 : 10000);
                    //	for (int m = 0; m < 3; m++)
                    //	{
                    //		for (int n = 0; n < 3; n++)
                    //		{

                    //			StructTile tile3 = tiles[Index(point2.X +m, point2.Y + n)];
                    //			tile3.type = 471;
                    //			tile3.frameX = (short)((flag ? 54 : 0) + m * 18);
                    //			tile3.frameY = (short)(n * 18);
                    //		}
                    //	}
                    //	if (TEWeaponsRack.Place(point2.X, point2.Y) != -1 && flag2)
                    //	{
                    //		TEWeaponsRack.TryPlacing(point2.X, point2.Y, tileEntNum2, prefix, 1);
                    //	}
                    //}
                    #endregion
                    #region ClearTempTiles
                    //ClearTempTiles();
                    //void ClearTempTiles()					
                    //{
                    //	for (int i = 0; i < Main.maxTilesX; i++)
                    //	{
                    //		for (int j = 0; j < Main.maxTilesY; j++)
                    //		{
                    //			if (Main.tile[i, j].type == 127 || Main.tile[i, j].type == 504)
                    //			{
                    //				#region KillTile
                    //				WorldGen.KillTile(i, j);

                    //				public static void KillTile(int i, int j, bool fail = false, bool effectOnly = false, bool noItem = false)
                    //				{
                    //					if (i < 0 || j < 0 || i >= Main.maxTilesX || j >= Main.maxTilesY)
                    //					{
                    //						return;
                    //					}
                    //					Tile tile = Main.tile[i, j];
                    //					if (tile == null)
                    //					{
                    //						tile = new Tile();
                    //						Main.tile[i, j] = tile;
                    //					}
                    //					if (!tile.active())
                    //					{
                    //						return;
                    //					}
                    //					if (j >= 1 && Main.tile[i, j - 1] == null)
                    //					{
                    //						Main.tile[i, j - 1] = new Tile();
                    //					}
                    //					int num = CheckTileBreakability(i, j);
                    //					if (num == 1)
                    //					{
                    //						fail = true;
                    //					}
                    //					if (num == 2)
                    //					{
                    //						return;
                    //					}
                    //					if (gen)
                    //					{
                    //						noItem = true;
                    //					}
                    //					if (!effectOnly && !stopDrops)
                    //					{
                    //						if (!noItem && FixExploitManEaters.SpotProtected(i, j))
                    //						{
                    //							return;
                    //						}
                    //						if (!gen && !Main.gameMenu)
                    //						{
                    //							KillTile_PlaySounds(i, j, fail, tile);
                    //						}
                    //					}
                    //					if (tile.type == 128 || tile.type == 269)
                    //					{
                    //						int num2 = i;
                    //						int num3 = tile.frameX;
                    //						int num4;
                    //						for (num4 = tile.frameX; num4 >= 100; num4 -= 100)
                    //						{
                    //						}
                    //						while (num4 >= 36)
                    //						{
                    //							num4 -= 36;
                    //						}
                    //						if (num4 == 18)
                    //						{
                    //							num3 = Main.tile[i - 1, j].frameX;
                    //							num2--;
                    //						}
                    //						if (num3 >= 100)
                    //						{
                    //							int num5 = 0;
                    //							while (num3 >= 100)
                    //							{
                    //								num3 -= 100;
                    //								num5++;
                    //							}
                    //							int num6 = Main.tile[num2, j].frameY / 18;
                    //							if (num6 == 0)
                    //							{
                    //								Item.NewItem(i * 16, j * 16, 16, 16, Item.headType[num5]);
                    //							}
                    //							if (num6 == 1)
                    //							{
                    //								Item.NewItem(i * 16, j * 16, 16, 16, Item.bodyType[num5]);
                    //							}
                    //							if (num6 == 2)
                    //							{
                    //								Item.NewItem(i * 16, j * 16, 16, 16, Item.legType[num5]);
                    //							}
                    //							for (num3 = Main.tile[num2, j].frameX; num3 >= 100; num3 -= 100)
                    //							{
                    //							}
                    //							Main.tile[num2, j].frameX = (short)num3;
                    //						}
                    //					}
                    //					if (tile.type == 334)
                    //					{
                    //						int num7 = i;
                    //						int frameX = tile.frameX;
                    //						int num8 = tile.frameX;
                    //						int num9 = 0;
                    //						while (num8 >= 5000)
                    //						{
                    //							num8 -= 5000;
                    //							num9++;
                    //						}
                    //						if (num9 != 0)
                    //						{
                    //							num8 = (num9 - 1) * 18;
                    //						}
                    //						num8 %= 54;
                    //						if (num8 == 18)
                    //						{
                    //							frameX = Main.tile[i - 1, j].frameX;
                    //							num7--;
                    //						}
                    //						if (num8 == 36)
                    //						{
                    //							frameX = Main.tile[i - 2, j].frameX;
                    //							num7 -= 2;
                    //						}
                    //						if (frameX >= 5000)
                    //						{
                    //							int num10 = frameX % 5000;
                    //							num10 -= 100;
                    //							int frameX2 = Main.tile[num7 + 1, j].frameX;
                    //							frameX2 = ((frameX2 < 25000) ? (frameX2 - 10000) : (frameX2 - 25000));
                    //							if (Main.netMode != 1)
                    //							{
                    //								Item item = new Item();
                    //								item.netDefaults(num10);
                    //								item.Prefix(frameX2);
                    //								int num11 = Item.NewItem(i * 16, j * 16, 16, 16, num10, 1, noBroadcast: true);
                    //								item.position = Main.item[num11].position;
                    //								Main.item[num11] = item;
                    //								NetMessage.SendData(21, -1, -1, null, num11);
                    //							}
                    //							frameX = Main.tile[num7, j].frameX;
                    //							int num12 = 0;
                    //							while (frameX >= 5000)
                    //							{
                    //								frameX -= 5000;
                    //								num12++;
                    //							}
                    //							if (num12 != 0)
                    //							{
                    //								frameX = (num12 - 1) * 18;
                    //							}
                    //							Main.tile[num7, j].frameX = (short)frameX;
                    //							Main.tile[num7 + 1, j].frameX = (short)(frameX + 18);
                    //						}
                    //					}
                    //					if (tile.type == 395)
                    //					{
                    //						int num13 = TEItemFrame.Find(i - tile.frameX % 36 / 18, j - tile.frameY % 36 / 18);
                    //						if (num13 != -1 && ((TEItemFrame)TileEntity.ByID[num13]).item.stack > 0)
                    //						{
                    //							((TEItemFrame)TileEntity.ByID[num13]).DropItem();
                    //							if (Main.netMode != 2)
                    //							{
                    //								Main.LocalPlayer.InterruptItemUsageIfOverTile(395);
                    //							}
                    //							return;
                    //						}
                    //					}
                    //					if (tile.type == 471)
                    //					{
                    //						int num14 = TEWeaponsRack.Find(i - tile.frameX % 54 / 18, j - tile.frameY % 54 / 18);
                    //						if (num14 != -1 && ((TEWeaponsRack)TileEntity.ByID[num14]).item.stack > 0)
                    //						{
                    //							((TEWeaponsRack)TileEntity.ByID[num14]).DropItem();
                    //							if (Main.netMode != 2)
                    //							{
                    //								Main.LocalPlayer.InterruptItemUsageIfOverTile(471);
                    //							}
                    //							return;
                    //						}
                    //					}
                    //					if (tile.type == 520)
                    //					{
                    //						int num15 = TEFoodPlatter.Find(i, j);
                    //						if (num15 != -1 && ((TEFoodPlatter)TileEntity.ByID[num15]).item.stack > 0)
                    //						{
                    //							((TEFoodPlatter)TileEntity.ByID[num15]).DropItem();
                    //							if (Main.netMode != 2)
                    //							{
                    //								Main.LocalPlayer.InterruptItemUsageIfOverTile(520);
                    //							}
                    //							return;
                    //						}
                    //					}
                    //					if ((tile.type == 470 && (CheckTileBreakability2_ShouldTileSurvive(i, j) || fail)) || (tile.type == 475 && (CheckTileBreakability2_ShouldTileSurvive(i, j) || fail)))
                    //					{
                    //						return;
                    //					}
                    //					int num16 = KillTile_GetTileDustAmount(fail, tile);
                    //					for (int k = 0; k < num16; k++)
                    //					{
                    //						KillTile_MakeTileDust(i, j, tile);
                    //					}
                    //					if (effectOnly)
                    //					{
                    //						return;
                    //					}
                    //					AttemptFossilShattering(i, j, tile, fail);
                    //					if (fail)
                    //					{
                    //						if (Main.netMode != 1 && TileID.Sets.IsShakeable[tile.type])
                    //						{
                    //							ShakeTree(i, j);
                    //						}
                    //						if (tile.type == 2 || tile.type == 23 || tile.type == 109 || tile.type == 199 || tile.type == 477 || tile.type == 492)
                    //						{
                    //							tile.type = 0;
                    //						}
                    //						if (tile.type == 60 || tile.type == 70)
                    //						{
                    //							tile.type = 59;
                    //						}
                    //						if (Main.tileMoss[tile.type])
                    //						{
                    //							tile.type = 1;
                    //						}
                    //						if (TileID.Sets.tileMossBrick[tile.type])
                    //						{
                    //							tile.type = 38;
                    //						}
                    //						SquareTileFrame(i, j);
                    //						return;
                    //					}
                    //					if (Main.getGoodWorld && Main.netMode != 1 && tile.type == 57)
                    //					{
                    //						for (int l = 0; l < 8; l++)
                    //						{
                    //							int maxValue = 2;
                    //							int num17 = i;
                    //							int num18 = j;
                    //							switch (l)
                    //							{
                    //								case 0:
                    //									num17--;
                    //									break;
                    //								case 1:
                    //									num17++;
                    //									break;
                    //								case 2:
                    //									num18--;
                    //									break;
                    //								case 3:
                    //									num18++;
                    //									break;
                    //								case 4:
                    //									num17--;
                    //									num18--;
                    //									break;
                    //								case 5:
                    //									num17++;
                    //									num18--;
                    //									break;
                    //								case 6:
                    //									num17--;
                    //									num18++;
                    //									break;
                    //								case 7:
                    //									num17++;
                    //									num18++;
                    //									break;
                    //							}
                    //							Tile tile2 = Main.tile[num17, num18];
                    //							if (tile2.active() && genRand.Next(maxValue) == 0 && tile2.type == 57 && !SolidTile(num17, num18 + 1))
                    //							{
                    //								KillTile(num17, num18, fail: false, effectOnly: false, noItem: true);
                    //								int num19 = Projectile.NewProjectile(GetProjectileSource_TileBreak(num17, num18), num17 * 16 + 8, num18 * 16 + 8, 0f, 0.41f, 40, 15, 0f, Main.myPlayer);
                    //								Main.projectile[num19].netUpdate = true;
                    //							}
                    //						}
                    //					}
                    //					if (Main.netMode != 1 && tile.type >= 481 && tile.type <= 483)
                    //					{
                    //						for (int m = 0; m < 8; m++)
                    //						{
                    //							int num20 = 6;
                    //							int num21 = i;
                    //							int num22 = j;
                    //							switch (m)
                    //							{
                    //								case 0:
                    //									num21--;
                    //									break;
                    //								case 1:
                    //									num21++;
                    //									break;
                    //								case 2:
                    //									num22--;
                    //									num20 /= 2;
                    //									break;
                    //								case 3:
                    //									num22++;
                    //									break;
                    //								case 4:
                    //									num21--;
                    //									num22--;
                    //									break;
                    //								case 5:
                    //									num21++;
                    //									num22--;
                    //									break;
                    //								case 6:
                    //									num21--;
                    //									num22++;
                    //									break;
                    //								case 7:
                    //									num21++;
                    //									num22++;
                    //									break;
                    //							}
                    //							Tile tile3 = Main.tile[num21, num22];
                    //							if (tile3.active() && genRand.Next(num20) == 0 && tile3.type >= 481 && tile3.type <= 483)
                    //							{
                    //								tile.active(active: false);
                    //								KillTile(num21, num22, fail: false, effectOnly: false, noItem: true);
                    //							}
                    //						}
                    //						int type = tile.type - 481 + 736;
                    //						int damage = 20;
                    //						ProjectileSource_TileBreak projectileSource_TileBreak = GetProjectileSource_TileBreak(i, j);
                    //						if (Main.netMode == 0)
                    //						{
                    //							Projectile.NewProjectile(projectileSource_TileBreak, i * 16 + 8, j * 16 + 8, 0f, 0.41f, type, damage, 0f, Main.myPlayer);
                    //						}
                    //						else if (Main.netMode == 2)
                    //						{
                    //							int num23 = Projectile.NewProjectile(projectileSource_TileBreak, i * 16 + 8, j * 16 + 8, 0f, 0.41f, type, damage, 0f, Main.myPlayer);
                    //							Main.projectile[num23].netUpdate = true;
                    //						}
                    //					}
                    //					if (!CheckTileBreakability2_ShouldTileSurvive(i, j))
                    //					{
                    //						if (tile.type == 51 && tile.wall == 62 && genRand.Next(4) != 0)
                    //						{
                    //							noItem = true;
                    //						}
                    //						if (!noItem && !stopDrops && Main.netMode != 1)
                    //						{
                    //							KillTile_DropBait(i, j, tile);
                    //							KillTile_DropItems(i, j, tile);
                    //						}
                    //						if (Main.netMode != 2)
                    //						{
                    //							AchievementsHelper.NotifyTileDestroyed(Main.player[Main.myPlayer], tile.type);
                    //						}
                    //						tile.active(active: false);
                    //						tile.halfBrick(halfBrick: false);
                    //						tile.frameX = -1;
                    //						tile.frameY = -1;
                    //						tile.color(0);
                    //						tile.frameNumber(0);
                    //						if (tile.type == 58 && j > Main.UnderworldLayer)
                    //						{
                    //							tile.lava(lava: true);
                    //							tile.liquid = 128;
                    //						}
                    //						else if (tile.type == 419)
                    //						{
                    //							Wiring.PokeLogicGate(i, j + 1);
                    //						}
                    //						else if (TileID.Sets.BlocksWaterDrawingBehindSelf[tile.type])
                    //						{
                    //							SquareWallFrame(i, j);
                    //						}
                    //						tile.type = 0;
                    //						tile.inActive(inActive: false);
                    //						SquareTileFrame(i, j);
                    //					}
                    //				}
                    //				#endregion
                    //			}
                    //		}
                    //	}
                    //}
                    #endregion
                    //WorldGen.gen = true;
                    #region LiquidLoading/Settiling
                    //WorldGen.waterLine = Main.maxTilesY;
                    //Liquid.QuickWater(2);
                    //WorldGen.WaterCheck();
                    //int liquidChecks = 0;
                    //Liquid.quickSettle = true;
                    //int totalLiquids = Liquid.numLiquid + LiquidBuffer.numLiquidBuffer;
                    //float progressAuxiliary = 0f;
                    //while (Liquid.numLiquid > 0 && liquidChecks < 100000)
                    //{
                    //	liquidChecks++;
                    //	float liquidProgress = (float)(totalLiquids - (Liquid.numLiquid + LiquidBuffer.numLiquidBuffer)) / (float)totalLiquids;
                    //	if (Liquid.numLiquid + LiquidBuffer.numLiquidBuffer > totalLiquids)
                    //	{
                    //		totalLiquids = Liquid.numLiquid + LiquidBuffer.numLiquidBuffer;
                    //	}
                    //	if (liquidProgress > progressAuxiliary)
                    //	{
                    //		progressAuxiliary = liquidProgress;
                    //	}
                    //	else
                    //	{
                    //		liquidProgress = progressAuxiliary;
                    //	}
                    //	//Main.oldStatusText = Lang.gen[27].Value + " " + (int)(liquidProgress * 100f / 2f + 50f) + "%";
                    //	Liquid.UpdateLiquid();
                    //}
                    //Liquid.quickSettle = false;
                    #endregion
                    /*Main.weatherCounter = WorldGen.genRand.Next(3600, 18000);
					Cloud.resetClouds();
					WorldGen.WaterCheck();
					WorldGen.gen = false;
					NPC.setFireFlyChance();*/
                    /*if (Main.slimeRainTime > 0.0)
					{
						Main.StartSlimeRain(announce: false);
					}*/
                    NPC.SetWorldSpecificMonstersByWorldID();
                    sw.Stop();
                    Console.Write($"[FakeProvider] Loaded world in {sw.Elapsed}");
                }
                catch (Exception lastThrownLoadException)
                {
                    Console.WriteLine($"{lastThrownLoadException.Message}: {lastThrownLoadException.StackTrace}");
                    WorldGen.loadFailed = true;
                    WorldGen.loadSuccess = false;
                    try
                    {
                        Console.WriteLine($"Error on FastLoadWorld: Falling back to vanilla LoadWorld");
                        LoadWorldDirect(false);
                        reader.Close();
                    }
                    catch
                    {
                    }
                }
                #endregion
            }
            catch (Exception ex)
            {
                Console.WriteLine($"{ex.Message}: {ex.StackTrace}");
                Console.WriteLine($"Error on FastLoadWorld: Falling back to vanilla LoadWorld");
                LoadWorldDirect(false);
            }

        }
        #endregion
    }
}

