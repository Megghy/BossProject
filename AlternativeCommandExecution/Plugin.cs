using Terraria;
using TerrariaApi.Server;
using TShockAPI;
using TShockAPI.Hooks;

namespace AlternativeCommandExecution
{
    [ApiVersion(2, 1)]
    public partial class Plugin : TerrariaPlugin
    {
        public override string Name => GetType().Namespace;

        public override string Author => "MistZZT";

        public override Version Version => GetType().Assembly.GetName().Version;

        public Plugin(Main game) : base(game) { }

        public static Configuration Config { get; private set; }

        public override void Initialize()
        {
            void ReloadConfig()
            {
                Config = Configuration.Read();
                Config.Write();
                LoadShortCommands();
            }
            ReloadConfig();

            ServerApi.Hooks.ServerCommand.Register(this, OnServerCommand, 1000);
            ServerApi.Hooks.NetGetData.Register(this, OnGetData);
            ServerApi.Hooks.GamePostInitialize.Register(this, OnPostInit, -1000);

            ServerApi.Hooks.GameUpdate.Register(this, OnUpdate);

            GeneralHooks.ReloadEvent += args => ReloadConfig();
            PlayerHooks.PlayerChat += OnChat;
            PlayerHooks.PlayerCommand += OnCommand;
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                ServerApi.Hooks.ServerCommand.Deregister(this, OnServerCommand);
                ServerApi.Hooks.NetGetData.Deregister(this, OnGetData);
                ServerApi.Hooks.GamePostInitialize.Deregister(this, OnPostInit);

                ServerApi.Hooks.GameUpdate.Deregister(this, OnUpdate);
                PlayerHooks.PlayerChat -= OnChat;
                PlayerHooks.PlayerCommand -= OnCommand;
            }
            base.Dispose(disposing);
        }
        private static void OnCommand(PlayerCommandEventArgs args)
        {
            if (args.Handled)
                return;
            args.Handled = ShortCommand.ShortCommandUtil.RunCmd(args.CommandName, args.CommandPrefix, args.Player, args.Parameters.ToArray());
        }
        private static void OnPostInit(EventArgs args)
        {
            SwitchCmd_OnPostInit();
        }

        private static void OnGetData(GetDataEventArgs args)
        {
            if (args.Handled)
            {
                return;
            }

            var player = TShock.Players[args.Msg.whoAmI];
            if (player == null || !player.ConnectionAlive)
            {
                return;
            }

            using (var data = new MemoryStream(args.Msg.readBuffer, args.Index, args.Length - 1))
            {
                switch (args.MsgID)
                {
                    case PacketTypes.HitSwitch:
                        OnHitSwitch(data, player);
                        break;
                }
            }


        }
    }
}
