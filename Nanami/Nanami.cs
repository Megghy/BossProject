using Microsoft.Xna.Framework;
using System.Reflection;
using System.Text;
using System.Timers;
using Terraria;
using TerrariaApi.Server;
using TShockAPI;
using TShockAPI.Hooks;
using Timer = System.Timers.Timer;

namespace Nanami
{
    [ApiVersion(2, 1)]
    public class Nanami : TerrariaPlugin
    {
        public const string PvpAllow = "nanami-allow";

        public override string Name => "Nanami";
        public override string Author => "MistZZT";
        public override string Description => "A TShock-based plugin which collect statistics of players.";
        public override Version Version => Assembly.GetExecutingAssembly().GetName().Version;

        internal static Configuration Config;
        internal static PvpDataManager PvpDatas;
        private Timer _updateTextTimer;

        internal static NanamiListener Listener;

        public Nanami(Main game) : base(game) { }

        public override void Initialize()
        {
            ServerApi.Hooks.GameInitialize.Register(this, OnInitialize);
            ServerApi.Hooks.GamePostInitialize.Register(this, OnPostInitialize);
            ServerApi.Hooks.NetGreetPlayer.Register(this, OnGreetPlayer);
            ServerApi.Hooks.ServerLeave.Register(this, OnLeave);
            ServerApi.Hooks.PostWorldSave.Register(this, OnPostSaveWorld);

            GetDataHandlers.TogglePvp += OnPvpToggle;
            GeneralHooks.ReloadEvent += OnReload;

            PvpDatas = new PvpDataManager(TShock.DB);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                ServerApi.Hooks.GameInitialize.Deregister(this, OnInitialize);
                ServerApi.Hooks.GamePostInitialize.Deregister(this, OnPostInitialize);
                ServerApi.Hooks.NetGreetPlayer.Deregister(this, OnGreetPlayer);
                ServerApi.Hooks.ServerLeave.Deregister(this, OnLeave);
                ServerApi.Hooks.PostWorldSave.Deregister(this, OnPostSaveWorld);

                GetDataHandlers.TogglePvp -= OnPvpToggle;
                GeneralHooks.ReloadEvent -= OnReload;
                _updateTextTimer.Dispose();

                Listener?.Dispose();
            }
            base.Dispose(disposing);
        }

        private static void OnInitialize(EventArgs args)
        {
            Config = Configuration.Read(Configuration.FilePath);
            Config.Write(Configuration.FilePath);

            Commands.ChatCommands.Add(new Command("nanami.pvp.show", Show, "pvp", "战绩"));
            Commands.ChatCommands.Add(new Command("nanami.pvp.allow", SetPvpAllow, "pvpallow", "战绩可见") { AllowServer = false });

            Listener = new NanamiListener();
        }

        private void OnPostInitialize(EventArgs args)
        {
            _updateTextTimer = new Timer(1000)
            {
                AutoReset = true,
                Enabled = true
            };
            _updateTextTimer.Elapsed += OnTimerUpdate;
        }

        private int _timerCount;
        private void OnTimerUpdate(object sender, ElapsedEventArgs elapsedEventArgs)
        {
            if (!Config.AutoBroadcastBestKiller)
            {
                return;
            }

            if (++_timerCount < Config.AutoBroadcastSeconds)
            {
                return;
            }

            if (Main.player.Where(p => p?.active == true).All(p => !p.hostile))
            {
                return;
            }

            var max =
                (from player in TShock.Players
                 where player?.Active == true && player.RealPlayer && player.TPlayer.hostile
                 let data = PlayerPvpData.GetPlayerData(player.Index)
                 orderby data.KillStreak descending
                 select data).ToArray();

            var sb = new StringBuilder(new string('=', 10)).Append("PvP战绩排行").AppendLine(new string('=', 10));
            for (var i = 0; i < 3; ++i)
            {
                if (max.Length <= i)
                    break;
                sb
                    .Append($"第{(i + 1).ToChineseCharacterDigit()}名： {max[i].KillStreak.ToChineseCharacterDigit()}连杀".PadRight(13))
                    .Append(" —— ")
                    .AppendLine(TShock.Players[max.ElementAt(i).PlayerIndex].Name);

            }
            var sbText = sb.ToString().TrimEnd();

            foreach (var player in TShock.Players.Where(p => p != null && p.Active && p.RealPlayer && p.TPlayer.hostile))
            {
                player.SendMessage(sbText, Color.Orange);
            }

            _timerCount = 0;
        }

        private static void OnGreetPlayer(GreetPlayerEventArgs args)
        {
            var player = TShock.Players[args.Who];
            if (player == null)
                return;

            PlayerPvpData.LoadPlayerData(player);
            player.SetData(PvpAllow, true);
        }

        private static void OnLeave(LeaveEventArgs args)
        {
            var player = TShock.Players[args.Who];

            var data = PlayerPvpData.GetPlayerData(player);
            if (data == null)
                return;

            PvpDatas.Save(player.Account.ID, data);
        }

        private static void OnPostSaveWorld(WorldPostSaveEventArgs args)
        {
            foreach (var plr in TShock.Players.Where(p => p?.Active == true && p.TPlayer?.hostile == true))
            {
                var data = PlayerPvpData.GetPlayerData(plr);

                if (data == null)
                    continue;

                PvpDatas.Save(plr.Account.ID, data);
            }
        }

        private static void OnPvpToggle(object sender, GetDataHandlers.TogglePvpEventArgs args)
        {
            if (!args.Pvp)
            {
                return;
            }

            TShock.Players[args.PlayerId]
                .SendInfoMessage("你可以通过 {0} 查看你的战绩.", TShock.Utils.ColorTag("/pvp", Color.LightSkyBlue));
        }

        private static void Show(CommandArgs args)
        {
            if (args.Parameters.Count == 0 && !args.Player.RealPlayer)
            {
                args.Player.SendErrorMessage("只有玩家才能使用战绩.");
                return;
            }

            var player = args.Player;
            if (args.Parameters.Count > 0)
            {
                var players = TSPlayer.FindByNameOrID(string.Join(" ", args.Parameters));
                if (players.Count == 0)
                {
                    args.Player.SendErrorMessage("指定玩家无效!");
                    return;
                }
                if (players.Count > 1)
                {
                    args.Player.SendMultipleMatchError(players.Select(p => p.Name));
                    return;
                }
                player = players.Single();
                if (!player.GetData<bool>(PvpAllow) && !args.Player.HasPermission("nanami.pvp.showother"))
                {
                    args.Player.SendErrorMessage("{0} 已经禁止别人查看其战绩.", player.Name);
                    return;
                }
            }

            var dt = PlayerPvpData.GetPlayerData(player.Index);
            args.Player.SendInfoMessage($"{"---- {0}的PvP战绩 ----",38}", player.Name);
            args.Player.SendInfoMessage($"{"",11}* | 消灭 {dt.Eliminations,8} | {"连续消灭数目",6} {dt.KillStreak,8} |");
            args.Player.SendInfoMessage($"{"",11}* | 伤害 {dt.DamageDone,8} | {"总承受伤害量",6} {dt.Endurance,8} |");
            args.Player.SendInfoMessage($"{"",11}* | 死亡 {dt.Deaths,8} | {"最大连续消灭",6} {dt.BestKillStreak,8} |");
        }

        private static void SetPvpAllow(CommandArgs args)
        {
            var current = args.Player.GetData<bool>(PvpAllow);

            if (!current)
                args.Player.SendSuccessMessage("开启战绩可见.");
            if (current)
                args.Player.SendSuccessMessage("关闭战绩可见.");

            args.Player.SetData(PvpAllow, !current);
        }

        private static void OnReload(ReloadEventArgs e)
        {
            Config = Configuration.Read(Configuration.FilePath);
            Config.Write(Configuration.FilePath);
            e.Player.SendSuccessMessage("已重新载入 Nanami 配置.");
        }
    }
}
