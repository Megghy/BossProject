using MiniWorldPlugin.Commands;
using MiniWorldPlugin.Managers;
using MiniWorldPlugin.Services;
using Terraria;
using TerrariaApi.Server;
using TShockAPI;

namespace MiniWorldPlugin
{
    [ApiVersion(2, 1)]
    public class MiniWorldPlugin : TerrariaPlugin
    {
        public override string Name => "MiniWorld Plugin";
        public override Version Version => new(1, 0, 0);
        public override string Author => "Megghy";
        public override string Description => "一个用于管理迷你世界的插件。";
        public static MiniWorldPlugin Instance { get; private set; }

        public static NodeManager NodeManager => NodeManager.Instance;
        public static WorldManager WorldManager => WorldManager.Instance;

        public MiniWorldPlugin(Main game) : base(game)
        {
            Instance = this;
        }

        public override void Initialize()
        {
            _ = Config.Instance; // 加载配置

            ServerApi.Hooks.GamePostInitialize.Register(this, OnTShockInitialized);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                ServerApi.Hooks.GamePostInitialize.Deregister(this, OnTShockInitialized);

                // 清理 RPC 客户端连接
                RpcClientService.Instance.Dispose();
                NodeManager.Instance.Dispose();
                WorldManager.Instance.Dispose();
            }
            base.Dispose(disposing);
        }

        private static async void OnTShockInitialized(EventArgs args)
        {
            try
            {
                // 初始化管理器
                _ = NodeManager.Instance;

                // 连接到 RPC 服务器
                var connected = await RpcClientService.Instance.ConnectAsync(Config.Instance.RpcUrl);
                if (connected)
                {
                    TShock.Log.ConsoleInfo($"[MiniWorld] 已连接到节点 RPC 服务器: {Config.Instance.RpcUrl}");

                    // 启动自动重连
                    RpcClientService.Instance.StartAutoReconnect(Config.Instance.RpcUrl, TimeSpan.FromSeconds(30));
                }
                else
                {
                    TShock.Log.ConsoleWarn($"[MiniWorld] 无法连接到节点 RPC 服务器: {Config.Instance.RpcUrl}");
                    TShock.Log.ConsoleInfo("[MiniWorld] 将会自动尝试重连...");

                    // 即使初始连接失败也启动自动重连
                    RpcClientService.Instance.StartAutoReconnect(Config.Instance.RpcUrl, TimeSpan.FromSeconds(30));
                }

                // 初始化世界管理器
                await WorldManager.Instance.InitializeAsync();

                // 注册指令
                MWCommands.Register();

                TShockAPI.Hooks.PlayerHooks.PlayerCommand += OnCommand;
                ServerApi.Hooks.ServerLeave.Register(MiniWorldPlugin.Instance, OnServerLeave);

                TShock.Log.ConsoleInfo("[MiniWorld] 插件已加载。");
            }
            catch (Exception ex)
            {
                TShock.Log.ConsoleError($"[MiniWorld] 初始化时出错: {ex}");
            }
        }

        private static void OnServerLeave(LeaveEventArgs args)
        {
            var player = TShock.Players[args.Who];
            WorldManager.Instance.RemovePlayerWorld(player);
        }

        private static void OnCommand(TShockAPI.Hooks.PlayerCommandEventArgs args)
        {
            if (args.Player == null)
            {
                return;
            }

            if (WorldManager.Instance.GetPlayerWorldAndSession(args.Player) is { } data && data.World is not null)
            {
                var world = data.World;
                var session = data.Session;
                if (string.Equals(args.CommandName, "tp", StringComparison.OrdinalIgnoreCase))
                {
                    if (args.Parameters.Count == 0)
                    {
                        args.Player.SendErrorMessage("请输入玩家名称");
                        args.Handled = true;
                        return;
                    }
                    var targets = TShock.Players.FirstOrDefault(p => p.Name == args.Parameters[0]) is { } t ? [t] : TShock.Players.Where(p => p.Name.ToLower().Contains(args.Parameters[0].ToLower())).ToList();
                    if (targets.Count == 0)
                    {
                        args.Player.SendErrorMessage("玩家不存在");
                        args.Handled = true;
                    }
                    else if (targets.Count > 1)
                    {
                        args.Player.SendMultipleMatchError(targets.Select(p => p.Name));
                        args.Handled = true;
                    }
                    else
                    {
                        args.Handled = true;
                        var target = targets.First();
                        args.Player.SendErrorMessage($"玩家 [C/FF0000:{target.Name}] 位于迷你世界 {world.WorldName}(Id:{world.Id}) 中, 无法直接传送.");
                        args.Player.SendInfoMessage($"可以使用使用 [C/BCBCBC:/mw goi {world.Id}] 传送至 {world.WorldName}");
                    }
                }
                else if (session is not null)
                {
                    if (!session.TargetServer.GlobalCommand.Exists(c => string.Equals(c, args.CommandName, StringComparison.OrdinalIgnoreCase)))
                    {
                        args.Handled = true;
                        Console.WriteLine($"[MiniWorld] <{args.Player.Name}> 忽略转发中的玩家执行的命令: {args.CommandText}");
                    }
                }
            }
        }
    }
}
