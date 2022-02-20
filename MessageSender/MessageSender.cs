using Microsoft.Xna.Framework;
using System.Reflection;
using Terraria;
using Terraria.Utilities;
using TerrariaApi.Server;
using TShockAPI;

namespace MessageSender
{
    [ApiVersion(2, 1)]
    public class MessageSender : TerrariaPlugin
    {
        public override string Name => GetType().Name;

        public override string Author => "MistZZT";

        public override Version Version => Assembly.GetExecutingAssembly().GetName().Version;

        public override string Description => "Send message to players";

        public MessageSender(Main game) : base(game)
        {
            Order = 10;
        }

        public override void Initialize()
        {
            Commands.ChatCommands.Add(new Command("messagesender.sendmsg", SendMessage, "sendmsg")
            {
                AllowServer = false,
                HelpText = "Send message to player"
            });

            Commands.ChatCommands.Add(new Command("messagesender.sendother", SendMessageToOther, "sendother")
            {
                AllowServer = true,
                HelpText = "Send message to other players"
            });

            Commands.ChatCommands.Add(new Command("messagesender.sendct", SendCombatText, "sendct")
            {
                AllowServer = false,
                HelpText = "Send combat text to player"
            });

            Commands.ChatCommands.Add(new Command("messagesender.sendctother", SendCombatTextToOther, "sendctother")
            {
                AllowServer = true,
                HelpText = "Send combat text to other players"
            });

            Commands.ChatCommands.Add(new Command("messagesender.sendctpos", SendCombatTextToPosition, "sendctpos")
            {
                AllowServer = true,
                HelpText = "Send combat text to a specific position"
            });
        }

        private static void SendMessage(CommandArgs args)
        {
            if (args.Parameters.Count == 0)
            {
                args.Player.SendErrorMessage("Invalid syntax! Proper syntax: /sendmessage <Messages> [r] [g] [b]");
                return;
            }

            if (args.Parameters.Count >= 4)
            {
                var rgbs = args.Parameters.Skip(args.Parameters.Count - 3).ToArray();
                if (!byte.TryParse(rgbs[0], out var r) || !byte.TryParse(rgbs[1], out var g) || !byte.TryParse(rgbs[2], out var b))
                {
                    args.Player.SendInfoMessage(string.Join(" ", args.Parameters));
                }
                else
                {
                    args.Player.SendMessage(string.Join(" ", args.Parameters.GetRange(0, args.Parameters.Count - 3)), r, g, b);
                }
            }
            else
            {
                args.Player.SendInfoMessage(string.Join(" ", args.Parameters));
            }
        }

        private static void SendMessageToOther(CommandArgs args)
        {
            if (args.Parameters.Count == 0)
            {
                args.Player.SendErrorMessage("Invalid syntax! Proper syntax: /sendother <Player> <Messages> [r] [g] [b]");
                return;
            }

            var players = TSPlayer.FindByNameOrID(args.Parameters[0]);
            if (players.Count > 1)
            {
                args.Player.SendMultipleMatchError(players.Select(p => p.Name));
                return;
            }
            if (players.Count == 0)
            {
                args.Player.SendErrorMessage("Invalid player name!");
                return;
            }

            var player = players[0];
            if (args.Parameters.Count >= 5)
            {
                var rgbs = args.Parameters.Skip(args.Parameters.Count - 3).ToArray();
                if (!byte.TryParse(rgbs[0], out var r) || !byte.TryParse(rgbs[1], out var g) || !byte.TryParse(rgbs[2], out var b))
                {
                    player.SendInfoMessage(string.Join(" ", args.Parameters.Skip(1)));
                }
                else
                {
                    player.SendMessage(string.Join(" ", args.Parameters.GetRange(1, args.Parameters.Count - 4)), r, g, b);
                }
            }
            else
            {
                player.SendInfoMessage(string.Join(" ", args.Parameters.Skip(1)));
            }
        }

        private static void SendCombatText(CommandArgs args)
        {
            if (args.Parameters.Count == 0)
            {
                args.Player.SendErrorMessage("Invalid syntax! Proper syntax: /sendct [-b/--broadcast] <Messages> [r] [g] [b]");
                return;
            }

            var broadcast = false;
            if (string.Equals(args.Parameters[0], "-b", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(args.Parameters[0], "--broadcast", StringComparison.OrdinalIgnoreCase))
            {
                broadcast = true;
                args.Parameters.RemoveAt(0);
            }

            Color color;
            string text;

            if (args.Parameters.Count >= 4)
            {
                var rgbs = args.Parameters.Skip(args.Parameters.Count - 3).ToArray();

                if (!byte.TryParse(rgbs[0], out var r) || !byte.TryParse(rgbs[1], out var g) || !byte.TryParse(rgbs[2], out var b))
                {
                    text = string.Join(" ", args.Parameters);
                    color = Color.Yellow;
                }
                else
                {
                    text = string.Join(" ", args.Parameters.GetRange(0, args.Parameters.Count - 3));
                    color = new Color(r, g, b);
                }
            }
            else
            {
                text = string.Join(" ", args.Parameters);
                color = Color.Yellow;
            }

            SendCombatText(args.Player, text, color, broadcast);
        }

        private static void SendCombatTextToOther(CommandArgs args)
        {
            if (args.Parameters.Count == 0)
            {
                args.Player.SendErrorMessage("Invalid syntax! Proper syntax: /sendctother [-b/--broadcast] <Player> <Messages> [r] [g] [b]");
                return;
            }

            var broadcast = false;
            if (string.Equals(args.Parameters[0], "-b", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(args.Parameters[0], "--broadcast", StringComparison.OrdinalIgnoreCase))
            {
                broadcast = true;
                args.Parameters.RemoveAt(0);
            }

            var players = TSPlayer.FindByNameOrID(args.Parameters[0]);
            if (players.Count > 1)
            {
                args.Player.SendMultipleMatchError(players.Select(p => p.Name));
                return;
            }
            if (players.Count == 0)
            {
                args.Player.SendErrorMessage("Invalid player name!");
                return;
            }
            var player = players[0];

            Color color;
            string text;

            if (args.Parameters.Count >= 5)
            {
                var rgbs = args.Parameters.Skip(args.Parameters.Count - 3).ToArray();
                if (!byte.TryParse(rgbs[0], out var r) || !byte.TryParse(rgbs[1], out var g) || !byte.TryParse(rgbs[2], out var b))
                {
                    color = Color.Yellow;
                    text = args.Parameters[1];
                }
                else
                {
                    color = new Color(r, g, b);
                    text = string.Join(" ", args.Parameters.GetRange(1, args.Parameters.Count - 4));
                }
            }
            else
            {
                color = Color.Yellow;
                text = args.Parameters[1];
            }
            SendCombatText(player, text, color, broadcast);
        }

        private static void SendCombatTextToPosition(CommandArgs args)
        {
            if (args.Parameters.Count < 3)
            {
                args.Player.SendErrorMessage("Invalid syntax! Proper syntax: /sendctpos [-b/--broadcast] <tileX> <tileY> <Messages> [r] [g] [b]");
                return;
            }
            var broadcast = false;
            if (string.Equals(args.Parameters[0], "-b", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(args.Parameters[0], "--broadcast", StringComparison.OrdinalIgnoreCase))
            {
                broadcast = true;
                args.Parameters.RemoveAt(0);
            }
            if (!int.TryParse(args.Parameters[0], out var x) || !int.TryParse(args.Parameters[1], out var y))
            {
                args.Player.SendErrorMessage("Invalid position!");
                return;
            }
            for (var i = 0; i < 2; i++)
                args.Parameters.RemoveAt(0);

            Color color;
            string text;
            if (args.Parameters.Count >= 4)
            {
                var rgbs = args.Parameters.Skip(args.Parameters.Count - 3).ToArray();
                if (!byte.TryParse(rgbs[0], out var r) || !byte.TryParse(rgbs[1], out var g) || !byte.TryParse(rgbs[2], out var b))
                {
                    color = Color.Yellow;
                    text = args.Parameters[0];
                }
                else
                {
                    color = new Color(r, g, b);
                    text = args.Parameters[0]; ;
                }
            }
            else
            {
                color = Color.Yellow;
                text = args.Parameters[0]; ;
            }
            SendCombatText(args.Player, text, color, new Vector2(x * 16, y * 16), broadcast);
        }

        public static void SendCombatText(TSPlayer player, string text, Color color, bool broadcast = false)
        {
            SendCombatText(player, text, color, player.TPlayer.getRect(), broadcast);
        }

        public static void SendCombatText(TSPlayer player, string text, Color color, Vector2 position, bool broadcast = false)
        {
            SendCombatText(player, text, color, new Rectangle((int)position.X, (int)position.Y, 0, 0), broadcast);
        }

        public static void SendCombatText(TSPlayer player, string text, Color color, Rectangle location, bool broadcast = false)
        {
            var position = GetPosition(location);
            if (broadcast)
                TSPlayer.All.SendData(PacketTypes.CreateCombatTextExtended, text, (int)color.PackedValue, position.X, position.Y);
            else
                player.SendData(PacketTypes.CreateCombatTextExtended, text, (int)color.PackedValue, position.X, position.Y);
        }

        private static Vector2 GetPosition(Rectangle location)
        {
            var vector = Vector2.Zero;
            var position = new Vector2(
                location.X + location.Width * 0.5f - vector.X * 0.5f,
                location.Y + location.Height * 0.25f - vector.Y * 0.5f
            );

            if (Main.rand == null)
                Main.rand = new UnifiedRandom();
            position.X += Main.rand.Next(-(int)(location.Width * 0.5), (int)(location.Width * 0.5) + 1);
            position.Y += Main.rand.Next(-(int)(location.Height * 0.5), (int)(location.Height * 0.5) + 1);
            return position;
        }

    }
}
