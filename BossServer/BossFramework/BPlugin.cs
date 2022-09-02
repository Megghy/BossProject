using BossFramework.BAttributes;
using Bssom.Serializer;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Terraria;
using TerrariaApi.Server;
using TrProtocol;

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
            ServerApi.Plugins.Select(p => p.PluginAssembly)
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
        #endregion
        [SimpleTimer(Time = 1)]
        private static void OnSecondUpdate()
        {
            var packets = new List<Packet>();
        }
    }
}
