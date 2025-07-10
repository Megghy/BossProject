using System.Reflection;
using MultiSCore.API;
using MultiSCore.Commands;
using MultiSCore.Model;
using MultiSCore.Services;
using Terraria;
using TerrariaApi.Server;
using TShockAPI;
using TShockAPI.Hooks;

namespace MultiSCore
{
    [ApiVersion(2, 1)]
    public class MSCPlugin : TerrariaPlugin
    {
        public override string Name => "MultiSCore";
        public override string Author => "Cai";
        public override string Description => "一个简单的跨服传送插件";
        public override Version Version => Assembly.GetExecutingAssembly().GetName().Version;

        internal static MSCPlugin Instance;

        internal Config _config;
        internal SessionManager _sessionManager;
        internal NetworkService _networkService;
        internal MscCommand _mscCommand;

        public MSCPlugin(Main game) : base(game)
        {
            Instance = this;
        }

        public override void Initialize()
        {
            // 1. 加载配置
            string configPath = Path.Combine(TShock.SavePath, "MultiSCore", "MSCConfig.json");
            _config = Config.LoadFromFile(configPath);

            // 2. 初始化服务
            _sessionManager = new SessionManager(_config, Version);
            _networkService = new NetworkService(_config, _sessionManager);
            _mscCommand = new MscCommand(_config, _sessionManager, GetText);

            // 3. 初始化 API
            MSCAPI.Initialize(_sessionManager, _config);

            // 4. 注册钩子
            ServerApi.Hooks.GamePostInitialize.Register(this, OnPostInit);
            ServerApi.Hooks.ServerLeave.Register(this, OnServerLeave);
            ServerApi.Hooks.NetGreetPlayer.Register(this, OnGreetPlayer);
            GeneralHooks.ReloadEvent += OnReload;

            BossFramework.BNet.BossSocket.OnPacketReceived += _networkService.OnReceiveData;
            BossFramework.BNet.BossSocket.OnPacketSending += _networkService.OnSendData;

            // 5. 注册命令
            TShockAPI.Commands.ChatCommands.Add(new Command("msc.use", _mscCommand.OnCommand, "msc"));
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                ServerApi.Hooks.GamePostInitialize.Deregister(this, OnPostInit);
                ServerApi.Hooks.ServerLeave.Deregister(this, OnServerLeave);
                ServerApi.Hooks.NetGreetPlayer.Deregister(this, OnGreetPlayer);
                GeneralHooks.ReloadEvent -= OnReload;

                BossFramework.BNet.BossSocket.OnPacketReceived -= _networkService.OnReceiveData;
                BossFramework.BNet.BossSocket.OnPacketSending -= _networkService.OnSendData;
            }
            base.Dispose(disposing);
        }

        private void OnPostInit(EventArgs args)
        {
            // 使用反射安全地订阅事件，避免硬依赖
            var bossSocketType = Type.GetType("BossFramework.BNet.BossSocket, BossFramework");
            if (bossSocketType != null)
            {
                var onPacketReceived = bossSocketType.GetEvent("OnPacketReceived");
                if (onPacketReceived != null)
                {
                    var del = Delegate.CreateDelegate(onPacketReceived.EventHandlerType, _networkService, "OnReceiveData");
                    onPacketReceived.AddEventHandler(null, del);
                }

                var onPacketSending = bossSocketType.GetEvent("OnPacketSending");
                if (onPacketSending != null)
                {
                    var del = Delegate.CreateDelegate(onPacketSending.EventHandlerType, _networkService, "OnSendData");
                    onPacketSending.AddEventHandler(null, del);
                }
            }
        }

        private void OnGreetPlayer(GreetPlayerEventArgs args)
        {
            _networkService.OnGreetPlayer(args);
        }

        private void OnServerLeave(LeaveEventArgs args)
        {
            _sessionManager.OnPlayerLeave(args.Who);
        }

        private void OnReload(ReloadEventArgs args)
        {
            string configPath = Path.Combine(TShock.SavePath, "MultiSCore", "MSCConfig.json");
            _config = Config.LoadFromFile(configPath);
            // TODO: 热重载配置到各个服务中
            args.Player.SendSuccessMessage("[MultiSCore] 插件配置已重载。");
        }

        private string GetText(string key)
        {
            return _config.Language?.Value<string>(key) ?? key;
        }
    }
}
