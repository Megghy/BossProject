using System.Reflection;
using BossFramework.BAttributes;
using Bssom.Serializer;
using CSScriptLib;
using Terraria;
using TerrariaApi.Server;
using TrProtocol;
using TShockAPI;

namespace BossFramework
{
    [ApiVersion(2, 1)]
    public class BossPlugin : TerrariaPlugin
    {
        public static BossPlugin Instance { get; private set; }

        public BossPlugin(Main game) : base(game)
        {
            Instance = this;
            Order = int.MaxValue; //最后加载
        }
        public override string Name => "BossPlugin";
        public override string Author => "Megghy";
        public override string Description => "写给boss服务器的插件";
        public override Version Version => Assembly.GetExecutingAssembly().GetName().Version!;

        #region 初始化
        public override void Initialize()
        {
            FakeProvider.FakeProviderPlugin.FastWorldLoad = BConfig.Instance.FastLoadWorld;
            BssomSerializer.Serialize(this);
            AutoInit();
        }
        private void AutoInit()
        {
            var auto = new Dictionary<MethodInfo, AutoInitAttribute>();
            var loaded = new List<Assembly>();
            var ass = ServerApi.Plugins.Select(p => p.Plugin.GetType().Assembly).ToList();
            ServerApi.Plugins.Select(p => p.Plugin.GetType().Assembly)
                .Where(a => a != null)
                .ForEach(a =>
                {
                    if (!loaded.Contains(a))
                    {
                        a.GetTypes()
                            .ForEach(t => t.GetMethods(BindingFlags.Instance | BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public)
                            .ForEach(m =>
                            {
                                if (m.GetCustomAttribute<AutoInitAttribute>() is { } attr)
                                    auto.Add(m, attr);
                            }));

                        loaded.Add(a);
                    }
                });


            //SafeReferenceDomainAssemblies();
            //CSScript.Evaluator.ReferenceAssemblyOf<BEventArgs.BaseEventArgs>();
            foreach (var file in Directory.GetFiles(Path.Combine(Environment.CurrentDirectory, ServerApi.PluginsPath), "*.dll"))
            {
                CSScript.Evaluator.ReferenceAssembly(file);
                Console.WriteLine($"[CSScript] 添加引用: {Path.GetFileNameWithoutExtension(file)}");
            }

            auto.OrderBy(a => a.Value.Order).ForEach(kv =>
            {
                try
                {
                    var attr = kv.Value;
                    if (attr.PreInitMessage is { } pre)
                        BLog.Info(pre);
                    kv.Key.Invoke(this, null);
                    if (attr.PostInitMessage is { } post)
                        BLog.Info(post);
                }
                catch (Exception ex)
                {
                    BLog.Error($"加载 [{kv.Key.DeclaringType!.Name}.{kv.Key.Name}] 时发生错误{Environment.NewLine}{ex}");
                }
            });
            BLog.Success($"BossPlugin 加载完成");
        }

        static void SafeReferenceDomainAssemblies()
        {
            var evaluator = CSScript.Evaluator;
            var referencedAssemblies = new HashSet<string>();

            // 获取所有已加载的程序集
            var assemblies = AppDomain.CurrentDomain.GetAssemblies();

            foreach (var assembly in assemblies)
            {
                // 跳过动态程序集和没有物理位置的程序集
                if (assembly.IsDynamic || string.IsNullOrEmpty(assembly.Location))
                {
                    // 你可以在这里记录日志，看看是哪个程序集被跳过了
                    // Console.WriteLine($"Skipping in-memory assembly: {assembly.FullName}");
                    continue;
                }

                // 避免重复引用
                if (referencedAssemblies.Contains(assembly.FullName))
                    continue;

                // 添加引用
                evaluator.ReferenceAssembly(assembly);
                referencedAssemblies.Add(assembly.FullName);
            }
        }
        #endregion
        [SimpleTimer(Time = 1)]
        internal static void OnSecondUpdate()
        {
            var packets = new List<Packet>();
        }
    }
}
