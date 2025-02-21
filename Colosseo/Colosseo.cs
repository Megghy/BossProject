using System.Reflection;
using Microsoft.Xna.Framework;
using Terraria;
using TerrariaApi.Server;
using TShockAPI;

namespace Colosseo
{
    [ApiVersion(2, 1)]
    public class Colosseo : TerrariaPlugin
    {
        internal static Configuration Config;

        internal CommandDelegate VanillaSpawnMob;

        internal int[] PlayerCds = new int[256];

        private DateTime _lastCheck = DateTime.UtcNow;

        public override string Name => GetType().Name;

        public override string Author => "MistZZT";

        public override Version Version => Assembly.GetExecutingAssembly().GetName().Version;

        public override string Description => "biu";

        private int CurrentNpcAmount => Main.npc.Count((NPC x) => x != null && x.active && Config.VenueRegion.InArea(x.GetTileX(), x.GetTileY()));

        public Colosseo(Main game)
            : base(game)
        {
        }

        public override void Initialize()
        {
            ServerApi.Hooks.GameInitialize.Register(this, OnInit);
            ServerApi.Hooks.GamePostInitialize.Register(this, OnPostInit, -1000);
            ServerApi.Hooks.GameUpdate.Register(this, OnUpdate);
            ServerApi.Hooks.NetGreetPlayer.Register(this, OnGreet);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                ServerApi.Hooks.GameInitialize.Deregister(this, OnInit);
                ServerApi.Hooks.GamePostInitialize.Register(this, OnPostInit);
                ServerApi.Hooks.GameUpdate.Deregister(this, OnUpdate);
                ServerApi.Hooks.NetGreetPlayer.Deregister(this, OnGreet);
            }
            base.Dispose(disposing);
        }

        private void OnGreet(GreetPlayerEventArgs args)
        {
            PlayerCds[args.Who] = 0;
        }

        private void OnUpdate(EventArgs args)
        {
            if ((DateTime.UtcNow - _lastCheck).TotalSeconds >= 1.0)
            {
                foreach (TSPlayer item in TShock.Players.Where((TSPlayer p) => p?.Active ?? false))
                {
                    if (PlayerCds[item.Index] == 1)
                    {
                        item.SendMessage("冷却时间结束! 你可以召唤怪物了!", Color.Cyan);
                    }
                    int[] playerCds = PlayerCds;
                    int index = item.Index;
                    if (--playerCds[index] <= 0)
                    {
                        PlayerCds[item.Index] = 0;
                    }
                }
                _lastCheck = DateTime.UtcNow;
            }
            if (!Config.InitSuccess)
            {
                return;
            }
            foreach (NPC item2 in Main.npc.Where((NPC n) => n != null && n.active && !n.townNPC && Config.ClearRegion.InArea(n.GetTileX(), n.GetTileY()) && !Config.VenueRegion.InArea(n.GetTileX(), n.GetTileY())))
            {
                item2.active = false;
                item2.type = 0;
                TSPlayer.All.SendData(PacketTypes.NpcUpdate, "", item2.whoAmI);
            }
        }

        private static void OnPostInit(EventArgs args)
        {
            Config.LoadRegions();
        }

        private void OnInit(EventArgs args)
        {
            Config = Configuration.Read(Configuration.FilePath);
            Config.Write(Configuration.FilePath);
            Command command = Commands.ChatCommands.Find((Command c) => c.HasAlias("sm"));
            VanillaSpawnMob = command.CommandDelegate;
            int index = Commands.ChatCommands.IndexOf(command);
            Commands.ChatCommands.Remove(command);
            Commands.ChatCommands.Insert(index, new Command(new List<string>
            {
                "colosseo.player.spawnmob",
                Permissions.spawnmob
            }, FakeSpawnMob, "sm", "刷怪", "spawnmob")
            {
                AllowServer = false,
                HelpText = "在你周围生成小怪. -- fake"
            });
        }

        private void FakeSpawnMob(CommandArgs args)
        {
            if (args.Player.HasPermission(Permissions.spawnmob))
            {
                VanillaSpawnMob(args);
                return;
            }
            if (args.Parameters.Count < 1 || args.Parameters.Count > 2)
            {
                args.Player.SendErrorMessage("语法无效! 正确语法: /sm <怪物名/Id> [数量]");
                return;
            }
            if (args.Parameters[0].Length == 0)
            {
                args.Player.SendErrorMessage("怪物种类无效!!");
                return;
            }
            int result = 1;
            if (args.Parameters.Count == 2 && !int.TryParse(args.Parameters[1], out result))
            {
                args.Player.SendErrorMessage("语法无效！正确语法: /sm <怪物名/Id> [数量]");
                return;
            }
            result = Math.Min(result, 200);
            int maxAmount = GetMaxAmount(args.Player);
            if (result > maxAmount)
            {
                args.Player.SendErrorMessage("单次最多生成{0}个怪物。", maxAmount);
            }
            else
            {
                if (!TryUseCmd(args.Player))
                {
                    return;
                }
                if (CurrentNpcAmount >= Config.MaxSpawnAmountInArea)
                {
                    args.Player.SendErrorMessage("当前区域内怪物数量太多，请稍后召唤。");
                    PlayerCds[args.Player.Index] = 0;
                }
                List<NPC> nPCByIdOrName = TShock.Utils.GetNPCByIdOrName(args.Parameters[0]);
                if (nPCByIdOrName.Count == 0)
                {
                    args.Player.SendErrorMessage("怪物种类无效！");
                    PlayerCds[args.Player.Index] = 0;
                    return;
                }
                if (nPCByIdOrName.Count > 1)
                {
                    args.Player.SendMultipleMatchError(nPCByIdOrName.Select((NPC n) => $"{Lang.GetNPCNameValue(n.type)}({n.type})"));
                    PlayerCds[args.Player.Index] = 0;
                    return;
                }
                NPC nPC = nPCByIdOrName[0];
                if (nPC.type < 1 || nPC.type >= 580)
                {
                    args.Player.SendErrorMessage("怪物种类无效！");
                    PlayerCds[args.Player.Index] = 0;
                }
                else if (!Config.HarmfulNpcs.Valid(nPC.type))
                {
                    TSPlayer.Server.SpawnNPC(nPC.type, nPC.FullName, result, args.Player.TileX, args.Player.TileY, 50, 20);
                    TSPlayer.All.SendMessage(TShock.Utils.ColorTag(args.Player.Name, Color.SpringGreen) + "召唤了" + TShock.Utils.ColorTag(result.ToString(), Color.GreenYellow) + "只" + TShock.Utils.ColorTag(Lang.GetNPCNameValue(nPC.type), Color.LightGreen) + "！", Color.LightBlue);
                }
                else
                {
                    args.Player.SendErrorMessage("禁止生成该怪物！");
                    PlayerCds[args.Player.Index] = 0;
                }
            }
        }

        public static int GetMaxAmount(TSPlayer player)
        {
            if (!player.IsLoggedIn)
            {
                return 0;
            }
            if (player.HasPermission("colosseo.spawn.infinite"))
            {
                return 200;
            }
            for (byte b = 20; b > 0; b = (byte)(b + 1))
            {
                if (player.HasPermission("colosseo.spawn." + b))
                {
                    return b;
                }
            }
            return Config.DefaultMaxSpawnAmount;
        }

        public int GetCd(TSPlayer player)
        {
            for (byte b = 0; b < 20; b = (byte)(b + 1))
            {
                if (player.HasPermission("colosseo.spawncd." + b))
                {
                    return b;
                }
            }
            return Config.DefaultCd;
        }

        public bool TryUseCmd(TSPlayer player)
        {
            int index = player.Index;
            if (PlayerCds[index] > 0)
            {
                player.SendErrorMessage("{0}秒以后才能继续召唤。", PlayerCds[index]);
                return false;
            }
            PlayerCds[index] = GetCd(player);
            return true;
        }
    }
}
