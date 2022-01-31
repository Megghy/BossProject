using BossPlugin.BAttributes;
using OTAPI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Terraria;
using TerrariaApi.Server;
using TShockAPI.Hooks;

namespace BossPlugin
{
    [ApiVersion(2, 1)]
    public class BPlugin : TerrariaPlugin
    {
        public static BPlugin Instance { get; private set; }
        public BPlugin(Main game) : base(game)
        {
            Instance = this;
        }
        public override string Name => "BossPlugin";
        public override string Author => "Megghy";
        public override string Description => "写给boss服务器的插件";
        public override Version Version => Assembly.GetExecutingAssembly().GetName().Version;

        #region 初始化
        public override void Initialize()
        {
            AutoInit();
        }
        private void AutoInit()
        {
            var auto = new Dictionary<MethodInfo, AutoInitAttribute>();
            Assembly.GetExecutingAssembly()
                        .GetTypes()
                        .ForEach(t => t.GetMethods(BindingFlags.Instance | BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public)
                        .ForEach(m =>
                        {
                            if (m.GetCustomAttribute<AutoInitAttribute>() is { } attr)
                                auto.Add(m, attr);
                        }));
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
                    BLog.Error($"加载 [{kv.Key.DeclaringType.Name}.{kv.Key.Name}] 时发生错误{Environment.NewLine}{ex}");
                }
            });
            BLog.Success($"BossPlugin 加载完成");
        }
        [AutoInit("挂载所有Hook")]
        private void HandleHooks()
        {
            GeneralHooks.ReloadEvent += BNet.HookHandlers.ReloadHandler.OnReload;

            ServerApi.Hooks.NetGreetPlayer.Register(this, BNet.HookHandlers.PlayerGreetHandler.OnGreetPlayer);

            Hooks.Net.ReceiveData += BNet.PacketHandler.OnGetPacket;
        }
        #endregion
    }
}
