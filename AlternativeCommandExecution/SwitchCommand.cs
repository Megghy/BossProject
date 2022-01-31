using System;
using System.IO;
using System.IO.Streams;
using System.Linq;
using AlternativeCommandExecution.SwitchCommand;
using TShockAPI;
using Terraria;
using Terraria.ID;

namespace AlternativeCommandExecution
{
	partial class Plugin
	{
		internal static SwitchCmdManager Scs;

		private static void SwitchCmd_OnPostInit()
		{
			Scs = new SwitchCmdManager(TShock.DB);
			Scs.UpdateSwitchCommands();

			Commands.ChatCommands.Add(new Command("ace.sc.manage", SwitchCommand, "sc", "指令开关")
			{
				HelpText = "管理指令开关状态。"
			});
		}

		public static void OnHitSwitch(MemoryStream data, TSPlayer player)
		{
			var i = (int)data.ReadInt16();
			var j = (int)data.ReadInt16();

			if (i < 0 || j < 0 || i >= Main.maxTilesX || j >= Main.maxTilesY)
			{
				return;
			}

			var tile = Main.tile[i, j];

			if (tile.type == TileID.Lever)
			{
				if (tile.frameY == 0)
				{
					j++;
				}
				if (tile.frameX % 36 == 0)
				{
					i++;
				}
			}

			var info = SwitchCmdPlayerInfo.GetInfo(player);

			if (info.WaitingSelection)
			{
				info.Ss?.Invoke(i, j);
				info.WaitingSelection = false;
				info.Ss = null;
			}
			else
			{
				var sc = Scs.SwitchCmds.SingleOrDefault(s => s.X == i && s.Y == j);
				if (sc == null)
				{
					return;

				}
				if (!sc.TryUse(player))
				{
					player.SendErrorMessage("指令开关冷却中，无法使用。");
					return;
				}
				try
				{
					if (!sc.IgnorePermission)
					{
						ShortCommand.ShortCommandUtil.HandleCommand(player, sc.Command);
					}
					else
						ShortCommand.ShortCommandUtil.HandleCommandIgnorePermission(player, sc.Command);
				}
				catch (Exception e)
				{
					TShock.Log.ConsoleError("执行指令时出现异常，详细请看日志文件。");
					TShock.Log.Error(e.ToString());
				}
			}
		}

		private static void SwitchCommand(CommandArgs args)
		{
			var cmd = args.Parameters.Count > 0 ? args.Parameters[0].ToUpperInvariant() : "HELP";
			var sec = string.Join(" ", args.Parameters.Skip(1));
			var empty = string.IsNullOrWhiteSpace(sec);
			var info = SwitchCmdPlayerInfo.GetInfo(args.Player);

			switch (cmd)
			{
				case "ADD":
					if (empty)
					{
						args.Player.SendErrorMessage("语法无效！正确语法：/sc add <指令>");
						return;
					}

					info.WaitingSelection = true;
					info.Ss += (i, j) =>
					{
						Scs.Add(i, j, sec);
						args.Player.SendSuccessMessage("覆盖设置选中开关指令：{0}", sec);
					};
					args.Player.SendInfoMessage("触发一个开关/压力板以设定其开关指令状态。");
					break;
				case "DEL":
					info.WaitingSelection = true;
					info.Ss += (i, j) =>
					{
						Scs.Del(i, j);
						args.Player.SendSuccessMessage("清除选中开关指令状态完毕!");
					};
					args.Player.SendInfoMessage("触发一个开关/压力板以清除其开关指令状态。");
					break;
				case "IGNORE":
					if (empty)
					{
						args.Player.SendErrorMessage("语法无效！正确语法：/sc ignore <true/false>");
						return;
					}
					bool open;
					if (string.Equals("true", sec, StringComparison.OrdinalIgnoreCase))
					{
						open = true;
					}
					else if (string.Equals("false", sec, StringComparison.OrdinalIgnoreCase))
					{
						open = false;
					}
					else
					{
						args.Player.SendErrorMessage("语法无效！正确语法：/sc ignore <true/false>");
						return;
					}
					info.WaitingSelection = true;
					info.Ss += (i, j) =>
					{
						if (!Scs.SetIgnoreStatus(i, j, open))
						{
							args.Player.SendErrorMessage("开关非指令开关。");
						}
						else
						{
							args.Player.SendSuccessMessage("设置忽略权限模式完毕！");
						}
					};
					args.Player.SendInfoMessage("触发一个开关/压力板以设置忽略权限为{0}.", open ? "开启" : "关闭");
					break;
				case "ALLCD":
					if (empty)
					{
						args.Player.SendErrorMessage("语法无效！正确语法：/sc allcd <冷却时间(秒)>");
						return;
					}
					int seconds;
					if (!int.TryParse(sec, out seconds) || seconds < 0)
					{
						args.Player.SendErrorMessage("语法无效！正确语法：/sc allcd <冷却时间(秒)>");
						return;
					}
					info.WaitingSelection = true;
					info.Ss += (i, j) =>
					{
						if (!Scs.SetAllPlyCd(i, j, seconds))
						{
							args.Player.SendErrorMessage("开关非指令开关。");
						}
						else
						{
							args.Player.SendSuccessMessage("设置冷却时间为 {0}", seconds);
						}
					};
					args.Player.SendInfoMessage("触发一个开关/压力板以设置冷却时间为 {0}。", seconds);
					break;
				case "INFO":
					info.WaitingSelection = true;
					info.Ss += (i, j) =>
					{
						var sc = Scs.SwitchCmds.FirstOrDefault(s => s.X == i && s.Y == j);
						if (sc == null)
						{
							args.Player.SendErrorMessage("开关非指令开关。");
						}
						else
						{
							args.Player.SendInfoMessage("执行指令：{0}", string.Join(" ", sc.Command));
							args.Player.SendInfoMessage("全局冷却：{0}秒", sc.AllPlayerCdSecond);
							args.Player.SendInfoMessage("跳过权限：{0}", sc.IgnorePermission ? "开" : "关");
						}
					};
					args.Player.SendInfoMessage("触发一个开关/压力板以查看其状态。");
					break;
				case "CLEAR":
					Scs.ClearNonexistents();
					args.Player.SendSuccessMessage("已经清除无用指令开关。");
					break;
				case "HELP":
					int pageNumber;
					if (!PaginationTools.TryParsePageNumber(args.Parameters, 1, args.Player, out pageNumber))
					{
						return;
					}
					var list = new []
					{
						"add <指令> - 设定某开关的指令状态",
						"del - 删除某开关的指令状态",
						"ignore <true/false> - 是否跳过权限执行",
						"allcd <冷却时间秒> - 设置开关全局冷却时间",
						"info - 查看某开关的指令状态",
						"clear - 清除无效开关指令状态",
						"help [页码] - 获取帮助"
					};
					PaginationTools.SendPage(args.Player, pageNumber, list,
						new PaginationTools.Settings
						{
							HeaderFormat = "指令开关子指令说明 ({0}/{1}):",
							FooterFormat = "键入 {0}sc help {{0}} 以获取下一页列表.".SFormat(Commands.Specifier),
							NothingToDisplayString = "当前没有说明."
						});
					break;
				default:
					args.Player.SendErrorMessage("无效子指令！输入 /sc help 以查看可用指令。");
					break;
			}
		}

		private DateTime _lastCheck = DateTime.UtcNow;

		private void OnUpdate(EventArgs args)
		{
			if ((DateTime.UtcNow - _lastCheck).TotalSeconds >= 1)
			{
				foreach (var c in Scs.SwitchCmds)
				{
					c.Tick();
				}
				_lastCheck = DateTime.UtcNow;
			}
		}
	}
}
