#region Using
using System.Reflection;
using BossFramework;
using BossFramework.BCore;
using BossFramework.BModels;
using FakeProvider.Handlers;
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
using TrProtocol.Packets;
using TShockAPI;
#endregion

namespace FakeProvider
{
    [ApiVersion(2, 1)]
    public class FakeProviderPlugin : TerrariaPlugin
    {
        #region Data

        public static FakeProviderPlugin Instance { get; private set; }

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
            new Command("FakeProvider.Control", CommandHandler.FakeCommand, "fake")
        };

        #endregion

        #region Constructor

        public FakeProviderPlugin(Main game) : base(game)
        {
            Instance = this;
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

            // 初始化各个处理器
            SignChestHandler.Initialize();
            WorldSaveHandler.Initialize();
            NetworkHandler.Initialize();

            Commands.ChatCommands.AddRange(CommandList);
        }
        bool loadedHeader = false;
        private void WorldFile_LoadHeader(object? sender, HookEvents.Terraria.IO.WorldFile.LoadHeaderEventArgs e)
        {
            e.ContinueExecution = false;
            e.OriginalMethod.Invoke(e.reader);

            Console.WriteLine("[FakeProvider] Loading custom tile provider.");
            loadedHeader = true;
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
                //ServerApi.Hooks.WorldSave.Deregister(this, OnPreSaveWorld);

                // 清理各个处理器
                SignChestHandler.Dispose();
                WorldSaveHandler.Dispose();
                NetworkHandler.Dispose();
            }
            base.Dispose(Disposing);
        }

        #endregion



        #region OnPreLoadWorld

        private void OnPreLoadWorld(object? o, HookEvents.Terraria.IO.WorldFile.LoadWorldEventArgs args)
        {
            args.ContinueExecution = false;
            args.OriginalMethod.Invoke(args.loadFromCloud);

            OnPostLoadWorld();
        }

        #endregion
        #region OnPostLoadWorld

        private void OnPostLoadWorld()
        {
            if (!loadedHeader)
            {
                Console.WriteLine("指定地图文件不存在");
                Console.ReadLine();
                Environment.Exit(0);
            }
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
    }
}

