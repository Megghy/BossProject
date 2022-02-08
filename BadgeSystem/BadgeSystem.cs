using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Chat;
using Terraria.GameContent.NetModules;
using Terraria.Localization;
using Terraria.Net;
using Terraria.UI.Chat;
using TerrariaApi.Server;
using TShockAPI;
using TShockAPI.Hooks;

namespace BadgeSystem
{
	[ApiVersion(2, 1)]
	public sealed class BadgeSystem : TerrariaPlugin
	{
		internal static BadgeManager Badges;

		internal static Configuration Config;

		public override string Name => GetType().Namespace;

		public override string Author => "傻吊MistZZT";

		public override Version Version => GetType().Assembly.GetName().Version;

		public BadgeSystem(Main game)
			: base(game)
		{
		}

		public override void Initialize()
		{
			ServerApi.Hooks.ServerChat.Register(this, OnChat, 999);
			ServerApi.Hooks.NetGreetPlayer.Register(this, OnGreet);
			ServerApi.Hooks.GameInitialize.Register(this, OnInitialize);
		}

		protected override void Dispose(bool disposing)
		{
			if (disposing)
			{
				ServerApi.Hooks.ServerChat.Deregister(this, OnChat);
				ServerApi.Hooks.NetGreetPlayer.Deregister(this, OnGreet);
				ServerApi.Hooks.GameInitialize.Deregister(this, OnInitialize);
			}
			base.Dispose(disposing);
		}

		private static void OnInitialize(EventArgs args)
		{
			Badges = new BadgeManager(TShock.DB);
			ReloadConfig();
			GeneralHooks.ReloadEvent += delegate
			{
				ReloadConfig();
			};
			Commands.ChatCommands.Add(new Command("badgesys.manage.admin", AddBadge, "addbadge", "ab"));
			Commands.ChatCommands.Add(new Command("badgesys.manage.admin", RemoveBadge, "rmbadge", "rmb"));
			Commands.ChatCommands.Add(new Command("badgesys.manage.admin", NewBadge, "newbadge", "nb"));
			Commands.ChatCommands.Add(new Command("badgesys.manage.admin", DelBadge, "delbadge", "db"));
			Commands.ChatCommands.Add(new Command("badgesys.manage.player", BadgeInfo, "badgeinfo", "bi"));
			Commands.ChatCommands.Add(new Command("badgesys.manage.player", RemoveCurrentBadge, "rmcbadge", "rmcb"));
			Commands.ChatCommands.Add(new Command("badgesys.manage.player", AddCurrentBadge, "adcbadge", "adcb"));
			static void ReloadConfig()
			{
				Config = Configuration.Read();
				Config.Write();
			}
		}

		private static void OnGreet(GreetPlayerEventArgs args)
		{
			PlayerData.GetPlayerData(TShock.Players[args.Who]);
		}

		private static void OnChat(ServerChatEventArgs args)
		{
			if (args.Handled)
			{
				return;
			}
			TSPlayer tSPlayer = TShock.Players[args.Who];
			if (tSPlayer == null)
			{
				args.Handled = true;
				return;
			}
			PlayerData playerData = PlayerData.GetPlayerData(tSPlayer);
			if (!playerData.Any)
			{
				return;
			}
			args.Handled = true;

			string tshockText = args.Text;
			foreach (KeyValuePair<LocalizedText, ChatCommandId> localizedCommand in ChatManager.Commands._localizedCommands)
			{
				if (localizedCommand.Value._name == args.CommandId._name)
				{
					tshockText = (string.IsNullOrEmpty(tshockText) ? localizedCommand.Key.Value : (localizedCommand.Key.Value + " " + tshockText));
					break;
				}
			}
			if ((tshockText.StartsWith(TShock.Config.Settings.CommandSpecifier) || tshockText.StartsWith(TShock.Config.Settings.CommandSilentSpecifier)) && !string.IsNullOrWhiteSpace(tshockText.Substring(1)))
			{
				args.Handled = true;
				if (!Commands.HandleCommand(tSPlayer, tshockText))
				{
					tSPlayer.SendErrorMessage("无法分析命令，请与管理员联系");
				}
				return;
			}
			if (!tSPlayer.HasPermission(Permissions.canchat))
			{
				args.Handled = true;
				return;
			}
			if (tSPlayer.mute)
			{
				tSPlayer.SendErrorMessage("你已被禁言，无法发送消息");
				args.Handled = true;
				return;
			}
			if (!TShock.Config.Settings.EnableChatAboveHeads)
			{
				tshockText = string.Format(TShock.Config.Settings.ChatFormat, tSPlayer.Group.Name, playerData.Prefix, tSPlayer.Name, tSPlayer.Group.Suffix, args.Text);
				bool flag = PlayerHooks.OnPlayerChat(tSPlayer, args.Text, ref tshockText);
				args.Handled = true;
				if (!flag)
				{
					TShock.Utils.Broadcast(tshockText, tSPlayer.Group.R, tSPlayer.Group.G, tSPlayer.Group.B);
				}
				return;
			}
			Player player = Main.player[args.Who];
			string name = player.name;
			player.name = string.Format(TShock.Config.Settings.ChatAboveHeadsFormat, tSPlayer.Group.Name, tSPlayer.Group.Prefix, tSPlayer.Name, tSPlayer.Group.Suffix);
			NetMessage.SendData(4, -1, -1, NetworkText.FromLiteral(player.name), args.Who);
			player.name = name;
			if (PlayerHooks.OnPlayerChat(tSPlayer, args.Text, ref tshockText))
			{
				args.Handled = true;
				return;
			}
			NetPacket packet = NetTextModule.SerializeServerMessage(NetworkText.FromLiteral(tshockText), new Color(tSPlayer.Group.R, tSPlayer.Group.G, tSPlayer.Group.B), (byte)args.Who);
			NetManager.Instance.Broadcast(packet, args.Who);
			NetMessage.SendData(4, -1, -1, NetworkText.FromLiteral(name), args.Who);
			string text = $"<{string.Format(TShock.Config.Settings.ChatAboveHeadsFormat, tSPlayer.Group.Name, tSPlayer.Group.Prefix, tSPlayer.Name, tSPlayer.Group.Suffix)}> {tshockText}";
			tSPlayer.SendMessage(text, tSPlayer.Group.R, tSPlayer.Group.G, tSPlayer.Group.B);
			TSPlayer.Server.SendMessage(text, tSPlayer.Group.R, tSPlayer.Group.G, tSPlayer.Group.B);
			TShock.Log.Info("Broadcast: {0}", text);
			args.Handled = true;
		}

		private static void BadgeInfo(CommandArgs args)
		{
			TSPlayer tSPlayer = args.Player;
			if (args.Parameters.Count > 0)
			{
				string search = string.Join(" ", args.Parameters);
				List<TSPlayer> list = TSPlayer.FindByNameOrID(search);
				if (list.Count == 0)
				{
					args.Player.SendErrorMessage("未找到玩家。");
					return;
				}
				if (list.Count > 1)
				{
					args.Player.SendMultipleMatchError(list.Select((TSPlayer x) => x.Name));
					return;
				}
				tSPlayer = list.Single();
			}
			if (!tSPlayer.RealPlayer)
			{
				args.Player.SendErrorMessage("缺少玩家参数。");
				return;
			}
			PlayerData playerData = PlayerData.GetPlayerData(tSPlayer);
			args.Player.SendInfoMessage("拥有徽章：" + string.Join("", playerData.Total.Select((Badge x) => "[c/" + x.ColorHex + ":" + x.Content + "]")));
			args.Player.SendInfoMessage("佩戴徽章：" + playerData.Prefix);
		}

		private static void AddBadge(CommandArgs args)
		{
			if (args.Parameters.Count == 0)
			{
				args.Player.SendErrorMessage("语法错误！用法：/addbadge [玩家] <识别号>");
			}
			else if (args.Parameters.Count > 1)
			{
				List<TSPlayer> list = TSPlayer.FindByNameOrID(args.Parameters[0]);
				if (list.Count == 0)
				{
					args.Player.SendErrorMessage("未找到玩家。");
					return;
				}
				if (list.Count > 1)
				{
					args.Player.SendMultipleMatchError(list.Select((TSPlayer x) => x.Name));
					return;
				}
				TSPlayer player = list.Single();
				string str = string.Join(" ", args.Parameters.Skip(1));
				if (!Config.TryParse(str, out var b))
				{
					args.Player.SendErrorMessage("识别号错误。");
					return;
				}
				PlayerData playerData = PlayerData.GetPlayerData(player);
				playerData.Add(b);
				args.Player.SendSuccessMessage("完成添加徽章。");
			}
			else if (!args.Player.RealPlayer)
			{
				args.Player.SendErrorMessage("缺少玩家参数。");
			}
			else
			{
				string str2 = args.Parameters[0];
				if (!Config.TryParse(str2, out var b2))
				{
					args.Player.SendErrorMessage("识别号错误。");
					return;
				}
				PlayerData playerData2 = PlayerData.GetPlayerData(args.Player);
				playerData2.Add(b2);
				args.Player.SendSuccessMessage("完成添加徽章。");
			}
		}

		private static void NewBadge(CommandArgs args)
		{
			if (args.Parameters.Count == 0)
			{
				args.Player.SendErrorMessage("语法错误！用法：/newbadge <内容> <识别号> <颜色> 示例：/newbadge 「Crazy!」 crazy ff6a6a");
			}
			else if (args.Parameters.Count > 2)
			{
				string content = args.Parameters[0];
				string id = args.Parameters[1];
				string text = args.Parameters[2];
				if (!Badge.TryParse(text, out var _))
				{
					TShock.Log.ConsoleError("Invalid color string");
					args.Player.SendSuccessMessage("颜色代码错误");
				}
				Badge b = new Badge(content, id, text);
				TSPlayer[] players = TShock.Players;
				//TSPlayer[] array = players;
				//TSPlayer[] array2 = array;
				foreach (TSPlayer tSPlayer in players)
				{
					if (tSPlayer != null)
					{
						PlayerData playerData = PlayerData.GetPlayerData(tSPlayer);
						if (playerData.Total.Any((Badge i) => i.Identifier == b.Identifier))
						{
							playerData.Add(b);
						}
					}
				}
				Config.Badges.RemoveAll((Badge i) => i.Identifier == b.Identifier);
				Config.Badges.Add(b);
				Config.Write();
				args.Player.SendSuccessMessage("完成创建徽章。");
			}
			else if (args.Parameters.Count == 1)
			{
				args.Player.SendSuccessMessage("创建徽章出错,缺少参数:识别号 颜色");
			}
			else if (args.Parameters.Count == 2)
			{
				args.Player.SendSuccessMessage("创建徽章出错,缺少参数:颜色");
			}
			else
			{
				args.Player.SendSuccessMessage("草，创建野蛮徽章出错，，，");
			}
		}

		private static void DelBadge(CommandArgs args)
		{
			if (args.Parameters.Count == 0)
			{
				args.Player.SendErrorMessage("语法错误！用法：/delbadge <识别号>");
			}
			else if (args.Parameters.Count >= 1)
			{
				string str = args.Parameters[0];
				if (!Config.TryParse(str, out var b))
				{
					args.Player.SendErrorMessage("无对应徽章或识别号错误");
					return;
				}
				TSPlayer[] players = TShock.Players;
				//TSPlayer[] array = players;
				foreach (TSPlayer tSPlayer in players)
				{
					if (tSPlayer != null)
					{
						PlayerData.GetPlayerData(tSPlayer).Remove(b);
					}
				}
				Config.Badges.Remove(b);
				Config.Write();
				args.Player.SendSuccessMessage("完成删除徽章。");
			}
			else
			{
				args.Player.SendSuccessMessage("草，删除野蛮徽章出错，，，虽然是不可能运行到这里的");
			}
		}

		private static void RemoveBadge(CommandArgs args)
		{
			if (args.Parameters.Count == 0)
			{
				args.Player.SendErrorMessage("语法错误！用法：/rmbadge [玩家] <识别号>");
			}
			else if (args.Parameters.Count > 1)
			{
				List<TSPlayer> list = TSPlayer.FindByNameOrID(args.Parameters[0]);
				if (list.Count == 0)
				{
					args.Player.SendErrorMessage("未找到玩家。");
					return;
				}
				if (list.Count > 1)
				{
					args.Player.SendMultipleMatchError(list.Select((TSPlayer x) => x.Name));
					return;
				}
				TSPlayer player = list.Single();
				string str = string.Join(" ", args.Parameters.Skip(1));
				if (!Config.TryParse(str, out var b))
				{
					args.Player.SendErrorMessage("识别号错误。");
					return;
				}
				PlayerData playerData = PlayerData.GetPlayerData(player);
				playerData.Remove(b);
				args.Player.SendSuccessMessage("完成删除徽章。");
			}
			else if (!args.Player.RealPlayer)
			{
				args.Player.SendErrorMessage("缺少玩家参数。");
			}
			else
			{
				string str2 = args.Parameters[0];
				if (!Config.TryParse(str2, out var b2))
				{
					args.Player.SendErrorMessage("识别号错误。");
					return;
				}
				PlayerData playerData2 = PlayerData.GetPlayerData(args.Player);
				playerData2.Remove(b2);
				args.Player.SendSuccessMessage("完成删除徽章。");
			}
		}

		private static void RemoveCurrentBadge(CommandArgs args)
		{
			if (args.Parameters.Count == 0)
			{
				args.Player.SendErrorMessage("语法错误！用法：/rmcbadge [玩家] <识别号>");
			}
			else if (args.Parameters.Count > 1)
			{
				List<TSPlayer> list = TSPlayer.FindByNameOrID(args.Parameters[0]);
				if (list.Count == 0)
				{
					args.Player.SendErrorMessage("未找到玩家。");
					return;
				}
				if (list.Count > 1)
				{
					args.Player.SendMultipleMatchError(list.Select((TSPlayer x) => x.Name));
					return;
				}
				TSPlayer player = list.Single();
				string str = string.Join(" ", args.Parameters.Skip(1));
				if (!Config.TryParse(str, out var b))
				{
					args.Player.SendErrorMessage("识别号错误。");
					return;
				}
				PlayerData playerData = PlayerData.GetPlayerData(player);
				playerData.RemoveCurrent(b);
				args.Player.SendSuccessMessage("完成摘下徽章。");
			}
			else if (!args.Player.RealPlayer)
			{
				args.Player.SendErrorMessage("缺少玩家参数。");
			}
			else
			{
				string str2 = args.Parameters[0];
				if (!Config.TryParse(str2, out var b2))
				{
					args.Player.SendErrorMessage("识别号错误。");
					return;
				}
				PlayerData playerData2 = PlayerData.GetPlayerData(args.Player);
				playerData2.RemoveCurrent(b2);
				args.Player.SendSuccessMessage("完成摘下徽章。");
			}
		}

		private static void AddCurrentBadge(CommandArgs args)
		{
			if (args.Parameters.Count == 0)
			{
				args.Player.SendErrorMessage("语法错误！用法：/adcbadge <识别号>");
				return;
			}
			if (!args.Player.RealPlayer)
			{
				args.Player.SendErrorMessage("缺少玩家参数。");
				return;
			}
			string str = string.Join(" ", args.Parameters);
			if (!Config.TryParse(str, out var b))
			{
				args.Player.SendErrorMessage("识别号错误。");
				return;
			}
			PlayerData playerData = PlayerData.GetPlayerData(args.Player);
			if (!playerData.Total.Contains(b))
			{
				args.Player.SendErrorMessage("你没有此徽章。");
				return;
			}
			playerData.Add(b);
			args.Player.SendSuccessMessage("完成佩戴徽章。");
		}
	}
}
