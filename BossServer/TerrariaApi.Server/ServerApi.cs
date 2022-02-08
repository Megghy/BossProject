using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using Terraria;
using TerrariaApi.Reporting;

namespace TerrariaApi.Server
{
    // TODO: Maybe re-implement a reload functionality for plugins, but you'll have to load all assemblies into their own
    // AppDomain in order to unload them again later. Beware that having them in their own AppDomain might cause threading
    // problems as usual locks will only work in their own AppDomains.
    public static class ServerApi
    {
        public const string PluginsPath = "ServerPlugins";

        public static readonly Version ApiVersion = new(2, 1, 0, 0);
        private static Main game;
        private static readonly Dictionary<string, Assembly> loadedAssemblies = new();
        private static readonly List<PluginContainer> plugins = new();

        internal static readonly CrashReporter reporter = new();

        public static bool IgnoreVersion
        {
            get;
            set;
        }
        public static string ServerPluginsDirectoryPath => Path.Combine(Environment.CurrentDirectory, PluginsPath);
        public static ReadOnlyCollection<PluginContainer> Plugins
        {
            get { return new ReadOnlyCollection<PluginContainer>(plugins); }
        }
        public static HookManager Hooks
        {
            get;
            private set;
        }
        public static LogWriterManager LogWriter
        {
            get;
            private set;
        }
        public static ProfilerManager Profiler
        {
            get;
            private set;
        }
        public static bool IsWorldRunning
        {
            get;
            internal set;
        }
        public static bool RunningMono { get; private set; }
        public static bool ForceUpdate { get; private set; }
        public static bool UseAsyncSocketsInMono { get; private set; }

        static ServerApi()
        {
            Hooks = new HookManager();
            LogWriter = new LogWriterManager();
            Profiler = new ProfilerManager();

            UseAsyncSocketsInMono = false;
            ForceUpdate = false;
            Type t = Type.GetType("Mono.Runtime");
            RunningMono = (t != null);
            Main.SkipAssemblyLoad = true;
        }

        internal static void Initialize(string[] commandLineArgs, Main game)
        {
            Profiler.BeginMeasureServerInitTime();
            ServerApi.LogWriter.ServerWriteLine(
                string.Format("TerrariaApi - Server v{0} started.", ApiVersion), TraceLevel.Verbose);
            ServerApi.LogWriter.ServerWriteLine(
                "\tCommand line: " + Environment.CommandLine, TraceLevel.Verbose);
            ServerApi.LogWriter.ServerWriteLine(
                string.Format("\tOS: {0} (64bit: {1})", Environment.OSVersion, Environment.Is64BitOperatingSystem), TraceLevel.Verbose);
            ServerApi.LogWriter.ServerWriteLine(
                "\tMono: " + RunningMono, TraceLevel.Verbose);

            ServerApi.game = game;
            HandleCommandLine(commandLineArgs);

            if (!Directory.Exists(ServerPluginsDirectoryPath))
            {
                string lcDirectoryPath =
                    Path.Combine(Path.GetDirectoryName(ServerPluginsDirectoryPath), PluginsPath.ToLower());

                if (Directory.Exists(lcDirectoryPath))
                {
                    Directory.Move(lcDirectoryPath, ServerPluginsDirectoryPath);
                    LogWriter.ServerWriteLine("Case sensitive filesystem detected, serverplugins directory has been renamed.", TraceLevel.Warning);
                }
                else
                {
                    Directory.CreateDirectory(ServerPluginsDirectoryPath);
                }
            }

            LoadPlugins();
        }

        internal static void DeInitialize()
        {
            UnloadPlugins();
            Profiler.Deatch();
            LogWriter.Deatch();
        }

        internal static void HandleCommandLine(string[] parms)
        {
            Dictionary<string, string> args = Utils.ParseArguements(parms);

            bool isAutoCreating = false;

            foreach (KeyValuePair<string, string> arg in args)
            {
                switch (arg.Key.ToLower())
                {
                    case "-ignoreversion":
                        {
                            ServerApi.IgnoreVersion = true;
                            ServerApi.LogWriter.ServerWriteLine(
                                "Plugin versions are no longer being regarded, you are on your own! If problems arise, TShock developers will not help you with issues regarding this.",
                                TraceLevel.Warning);

                            break;
                        }
                    case "-forceupdate":
                        {
                            ServerApi.ForceUpdate = true;
                            ServerApi.LogWriter.ServerWriteLine(
                                "Forcing game updates regardless of players! This is experimental, and will cause constant CPU usage, you are on your own.",
                                TraceLevel.Warning);

                            break;
                        }
                    case "-asyncmono":
                        {
                            ServerApi.UseAsyncSocketsInMono = true;
                            ServerApi.LogWriter.ServerWriteLine(
                                "Forcing Mono to use asynchronous sockets.  This is highly experimental and may not work on all versions of Mono.",
                                TraceLevel.Warning);
                            break;
                        }
                    case "-players":
                        {
                            int playerCount;
                            if (!Int32.TryParse(arg.Value, out playerCount))
                            {
                                ServerApi.LogWriter.ServerWriteLine("Invalid player count. Using 8", TraceLevel.Warning);

                                playerCount = 8;
                            }

                            game.SetNetPlayers(playerCount);

                            break;
                        }
                    case "-maxplayers":
                        goto case "-players";
                    case "-pass":
                        {
                            Netplay.ServerPassword = arg.Value;

                            break;
                        }
                    case "-password":
                        goto case "-pass";
                    case "-worldname":
                        {
                            game.SetWorldName(arg.Value);

                            break;
                        }
                    case "-world":
                        {
                            game.SetWorld(arg.Value, false);

                            var full_path = Path.GetFullPath(arg.Value);
                            Main.WorldPath = Path.GetDirectoryName(full_path);
                            Main.worldName = Path.GetFileNameWithoutExtension(full_path);

                            break;
                        }
                    case "-motd":
                        {
                            game.NewMOTD(arg.Value);

                            break;
                        }
                    case "-banlist":
                        {
                            Netplay.BanFilePath = arg.Value;

                            break;
                        }
                    case "-autoshutdown":
                        {
                            game.EnableAutoShutdown();

                            break;
                        }
                    case "-secure":
                        {
                            Netplay.SpamCheck = true;

                            break;
                        }
                    case "-autocreate":
                        {
                            game.autoCreate(arg.Value);
                            isAutoCreating = true;

                            break;
                        }
                    case "-difficulty":
                        {
                            if (!isAutoCreating)
                            {
                                LogWriter.ServerWriteLine("Ignoring difficulty command line flag because server is starting in interactive mode without autocreate", TraceLevel.Warning);
                                continue;
                            }

                            // If the arg isn't an integer, or its an incorrect value, we want to ignore it
                            if (int.TryParse(arg.Value, out int dif))
                            {
                                if (dif >= 0 && dif <= 3)
                                {
                                    Main.GameMode = dif;
                                }
                            }
                            else
                            {
                                LogWriter.ServerWriteLine("Unexpected difficulty value. Expected values are 0-3.", TraceLevel.Warning);
                            }

                            break;
                        }
                    case "-loadlib":
                        {
                            game.loadLib(arg.Value);

                            break;
                        }
                    case "-crashdir":
                        CrashReporter.crashReportPath = arg.Value;
                        break;
                }
            }
        }

        internal static void LoadPlugins()
        {
            string ignoredPluginsFilePath = Path.Combine(ServerPluginsDirectoryPath, "ignoredplugins.txt");

            DangerousPluginDetector detector = new DangerousPluginDetector();

            List<string> ignoredFiles = new List<string>();
            if (File.Exists(ignoredPluginsFilePath))
                ignoredFiles.AddRange(File.ReadAllLines(ignoredPluginsFilePath));

            List<FileInfo> fileInfos = new DirectoryInfo(ServerPluginsDirectoryPath).GetFiles("*.dll").ToList();
            fileInfos.AddRange(new DirectoryInfo(ServerPluginsDirectoryPath).GetFiles("*.dll-plugin"));

            Dictionary<TerrariaPlugin, Stopwatch> pluginInitWatches = new Dictionary<TerrariaPlugin, Stopwatch>();

            var ts = new PluginContainer((TerrariaPlugin)Activator.CreateInstance(typeof(TShockAPI.TShock), game));
            plugins.Add(ts); //默认加载ts
            pluginInitWatches.Add(ts.Plugin, new());
            var boss = new PluginContainer((TerrariaPlugin)Activator.CreateInstance(typeof(BossFramework.BPlugin), game));
            plugins.Add(boss); //默认加载ts
            pluginInitWatches.Add(boss.Plugin, new());

            foreach (FileInfo fileInfo in fileInfos)
            {
                string fileNameWithoutExtension = Path.GetFileNameWithoutExtension(fileInfo.Name);
                if (ignoredFiles.Contains(fileNameWithoutExtension))
                {
                    LogWriter.ServerWriteLine(
                        string.Format("{0} was ignored from being loaded.", fileNameWithoutExtension), TraceLevel.Verbose);

                    continue;
                }

                try
                {
                    Assembly assembly;
                    // The plugin assembly might have been resolved by another plugin assembly already, so no use to
                    // load it again, but we do still have to verify it and create plugin instances.
                    if (!loadedAssemblies.TryGetValue(fileNameWithoutExtension, out assembly))
                    {
                        try
                        {
                            assembly = Assembly.LoadFrom(fileInfo.FullName);
                        }
                        catch (BadImageFormatException)
                        {
                            continue;
                        }
                        loadedAssemblies.Add(fileNameWithoutExtension, assembly);
                    }

                    foreach (Type type in assembly.GetExportedTypes())
                    {
                        if (!type.IsSubclassOf(typeof(TerrariaPlugin)) || !type.IsPublic || type.IsAbstract)
                            continue;
                        object[] customAttributes = type.GetCustomAttributes(typeof(ApiVersionAttribute), false);
                        if (customAttributes.Length == 0)
                            continue;

                        if (!IgnoreVersion)
                        {
                            var apiVersionAttribute = (ApiVersionAttribute)customAttributes[0];
                            Version apiVersion = apiVersionAttribute.ApiVersion;
                            if (apiVersion.Major != ApiVersion.Major || apiVersion.Minor != ApiVersion.Minor)
                            {
                                LogWriter.ServerWriteLine(
                                    string.Format("Plugin \"{0}\" is designed for a different Server API version ({1}) and was ignored.",
                                    type.FullName, apiVersion.ToString(2)), TraceLevel.Warning);

                                continue;
                            }
                        }

                        TerrariaPlugin pluginInstance;
                        try
                        {
                            Stopwatch initTimeWatch = new Stopwatch();
                            initTimeWatch.Start();

                            pluginInstance = (TerrariaPlugin)Activator.CreateInstance(type, game);

                            initTimeWatch.Stop();
                            pluginInitWatches.Add(pluginInstance, initTimeWatch);
                        }
                        catch (Exception ex)
                        {
                            // Broken plugins better stop the entire server init.
                            throw new InvalidOperationException(
                                string.Format("Could not create an instance of plugin class \"{0}\".", type.FullName), ex);
                        }
                        plugins.Add(new PluginContainer(pluginInstance));
                    }
                }
                catch (Exception ex)
                {
                    // Broken assemblies / plugins better stop the entire server init.
                    throw new InvalidOperationException(
                        string.Format("Failed to load assembly \"{0}\".", fileInfo.Name), ex);
                }
            }
            IOrderedEnumerable<PluginContainer> orderedPluginSelector =
                from x in Plugins
                orderby x.Plugin.Order, x.Plugin.Name
                select x;

            foreach (PluginContainer current in orderedPluginSelector)
            {
                Stopwatch initTimeWatch = pluginInitWatches[current.Plugin];
                initTimeWatch.Start();

                try
                {
                    current.Initialize();
                }
                catch (Exception ex)
                {
                    // Broken plugins better stop the entire server init.
                    throw new InvalidOperationException(string.Format(
                        "Plugin \"{0}\" has thrown an exception during initialization.", current.Plugin.Name), ex);
                }

                initTimeWatch.Stop();
                LogWriter.ServerWriteLine(string.Format(
                    "Plugin {0} v{1} (by {2}) initiated.", current.Plugin.Name, current.Plugin.Version, current.Plugin.Author),
                    TraceLevel.Info);
            }

            if (Profiler.WrappedProfiler != null)
            {
                foreach (var pluginWatchPair in pluginInitWatches)
                {
                    TerrariaPlugin plugin = pluginWatchPair.Key;
                    Stopwatch initTimeWatch = pluginWatchPair.Value;

                    Profiler.InputPluginInitTime(plugin, initTimeWatch.Elapsed);
                }
            }
        }

        internal static void UnloadPlugins()
        {
            var pluginUnloadWatches = new Dictionary<PluginContainer, Stopwatch>();
            foreach (PluginContainer pluginContainer in plugins)
            {
                Stopwatch unloadWatch = new Stopwatch();
                unloadWatch.Start();

                try
                {
                    pluginContainer.DeInitialize();
                }
                catch (Exception ex)
                {
                    LogWriter.ServerWriteLine(string.Format(
                        "Plugin \"{0}\" has thrown an exception while being deinitialized:\n{1}", pluginContainer.Plugin.Name, ex),
                        TraceLevel.Error);
                }

                unloadWatch.Stop();
                pluginUnloadWatches.Add(pluginContainer, unloadWatch);
            }

            foreach (PluginContainer pluginContainer in plugins)
            {
                Stopwatch unloadWatch = pluginUnloadWatches[pluginContainer];
                unloadWatch.Start();

                try
                {
                    pluginContainer.Dispose();
                }
                catch (Exception ex)
                {
                    LogWriter.ServerWriteLine(string.Format(
                        "Plugin \"{0}\" has thrown an exception while being disposed:\n{1}", pluginContainer.Plugin.Name, ex),
                        TraceLevel.Error);
                }

                unloadWatch.Stop();
                Profiler.InputPluginUnloadTime(pluginContainer.Plugin, unloadWatch.Elapsed);
            }
        }

        internal static Assembly CurrentDomain_AssemblyResolve(object sender, ResolveEventArgs args)
        {
            string fileName = args.Name.Split(',')[0];
            if (loadedAssemblies?.TryGetValue(fileName, out var target) == true)
                return target;
            try
            {

                string path = Path.Combine(ServerPluginsDirectoryPath, fileName + ".dll");
                if (!File.Exists(path))
                    path = Path.Combine(Environment.CurrentDirectory, "Lib", fileName + ".dll");
                if (!File.Exists(path))
                {
                    return null;
                }
                var assembly = Assembly.LoadFrom(path);
                loadedAssemblies?.Add(fileName, assembly);
                return assembly;
            }
            catch (Exception ex)
            {
                LogWriter.ServerWriteLine(
                    string.Format("Error on resolving assembly \"{0}.dll\":\n{1}", fileName, ex),
                    TraceLevel.Error);
            }
            return null;
        }
    }
}
