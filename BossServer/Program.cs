using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using ReLogic.OS;
using Terraria.ID;

namespace TerrariaApi.Server
{
    public class Program
    {
        /// <summary>
        /// Initialises any internal values before any server initialisation begins
        /// </summary>
        public static void InitialiseInternals()
        {
            ItemID.Sets.Explosives = ItemID.Sets.Factory.CreateBoolSet(new int[]
            {
				// Bombs
				ItemID.Bomb,
                ItemID.StickyBomb,
                ItemID.BouncyBomb,
                ItemID.BombFish,
                ItemID.DirtBomb,
                ItemID.DirtStickyBomb,
                ItemID.ScarabBomb,
				// Launchers
				ItemID.GrenadeLauncher,
                ItemID.RocketLauncher,
                ItemID.SnowmanCannon,
                ItemID.Celeb2,
				// Rockets
				ItemID.RocketII,
                ItemID.RocketIV,
                ItemID.ClusterRocketII,
                ItemID.MiniNukeII,
				// The following are classified as explosives untill we can figure out a better way.
				ItemID.DryRocket,
                ItemID.WetRocket,
                ItemID.LavaRocket,
                ItemID.HoneyRocket,
				// Explosives & misc
				ItemID.Dynamite,
                ItemID.Explosives,
                ItemID.StickyDynamite
            });

            //Set corrupt tiles to true, as they aren't in vanilla
            TileID.Sets.Corrupt[TileID.CorruptGrass] = true;
            TileID.Sets.Corrupt[TileID.CorruptPlants] = true;
            TileID.Sets.Corrupt[TileID.CorruptThorns] = true;
            TileID.Sets.Corrupt[TileID.CorruptIce] = true;
            TileID.Sets.Corrupt[TileID.CorruptHardenedSand] = true;
            TileID.Sets.Corrupt[TileID.CorruptSandstone] = true;
            TileID.Sets.Corrupt[TileID.Ebonstone] = true;
            TileID.Sets.Corrupt[TileID.Ebonsand] = true;

            //Same again for crimson
            TileID.Sets.Crimson[TileID.FleshBlock] = true;
            TileID.Sets.Crimson[TileID.CrimsonGrass] = true;
            TileID.Sets.Crimson[TileID.FleshIce] = true;
            TileID.Sets.Crimson[TileID.CrimsonPlants] = true;
            TileID.Sets.Crimson[TileID.Crimstone] = true;
            TileID.Sets.Crimson[TileID.Crimsand] = true;
            TileID.Sets.Crimson[TileID.CrimsonVines] = true;
            TileID.Sets.Crimson[TileID.CrimsonThorns] = true;
            TileID.Sets.Crimson[TileID.CrimsonHardenedSand] = true;
            TileID.Sets.Crimson[TileID.CrimsonSandstone] = true;

            //And hallow
            TileID.Sets.Hallow[TileID.HallowedGrass] = true;
            TileID.Sets.Hallow[TileID.HallowedPlants] = true;
            TileID.Sets.Hallow[TileID.HallowedPlants2] = true;
            TileID.Sets.Hallow[TileID.HallowedVines] = true;
            TileID.Sets.Hallow[TileID.HallowedIce] = true;
            TileID.Sets.Hallow[TileID.HallowHardenedSand] = true;
            TileID.Sets.Hallow[TileID.HallowSandstone] = true;
            TileID.Sets.Hallow[TileID.Pearlsand] = true;
            TileID.Sets.Hallow[TileID.Pearlstone] = true;
        }

        /// <summary>
        /// 1.4.4.2 introduced another static variable, which needs to be setup before any Main calls
        /// </summary>
        static void PrepareSavePath(string[] args)
        {
            Terraria.Program.LaunchParameters = Terraria.Utils.ParseArguements(args);
            Terraria.Program.SavePath = (Terraria.Program.LaunchParameters.ContainsKey("-savedirectory")
                ? Terraria.Program.LaunchParameters["-savedirectory"]
                : Platform.Get<IPathService>().GetStoragePath("Terraria"));
        }

        public static void Main(string[] args)
        {
            AppDomain.CurrentDomain.UnhandledException += UnhandledException;
            System.Runtime.Loader.AssemblyLoadContext.Default.Resolving += Default_Resolving;
            try
            {
                PrepareSavePath(args);
                InitialiseInternals();
                ServerApi.Hooks.AttachOTAPIHooks(args);

                // avoid any Terraria.Main calls here or the heaptile hook will not work.
                // this is because the hook is executed on the Terraria.Main static constructor,
                // and simply referencing it in this method will trigger the constructor.
                StartServer(args);

                ServerApi.DeInitialize();
            }
            catch (Exception ex)
            {
                Console.WriteLine("Server crashed due to an unhandled exception:\n" + ex, TraceLevel.Error);
            }
        }
        static Dictionary<string, Assembly> _cache = new Dictionary<string, Assembly>();
        static Assembly? Default_Resolving(System.Runtime.Loader.AssemblyLoadContext arg1, AssemblyName arg2)
        {
            if (arg2?.Name is null) return null;
            if (_cache.TryGetValue(arg2.Name, out Assembly? asm) && asm is not null) return asm;

            var loc = Path.Combine(AppContext.BaseDirectory, "Lib", arg2.Name + ".dll");
            if (File.Exists(loc))
                asm = arg1.LoadFromAssemblyPath(loc);

            loc = Path.ChangeExtension(loc, ".exe");
            if (File.Exists(loc))
                asm = arg1.LoadFromAssemblyPath(loc);

            if (asm is not null)
                _cache[arg2.Name] = asm;

            return asm;
        }
        static void StartServer(string[] args)
        {
            if (args.Any(x => x == "-skipassemblyload"))
            {
                Terraria.Main.SkipAssemblyLoad = true;
            }

            Terraria.WindowsLaunch.Main(args);
        }

        /// <summary>
        /// TShock sets up its own unhandled exception handler; this one is just to catch possible
        /// startup exceptions
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private static void UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            Console.WriteLine($"Unhandled exception\n{e}");
        }
    }
}