using System;
using System.IO;
using System.Linq;
using System.Reflection;
using OTAPI;
using Philosophyz.Hooks;
using Terraria;
using Terraria.GameContent.Events;
using Terraria.Localization;
using Terraria.Social;
using TerrariaApi.Server;
using TShockAPI;
using TShockAPI.Hooks;

namespace Philosophyz
{
	[ApiVersion(2, 1)]
	public class Philosophyz : TerrariaPlugin
	{
		private const bool DefaultFakeSscStatus = false;

		private const double DefaultCheckTime = 1.5d;

		public override string Name => Assembly.GetExecutingAssembly().GetName().Name;

		public override string Author => "MistZZT";

		public override string Description => "Dark";

		public override Version Version => Assembly.GetExecutingAssembly().GetName().Version;

		public Philosophyz(Main game) : base(game)
		{
			Order = 0; // 最早
		}

		internal PzRegionManager PzRegions;

		private OTAPI.Hooks.Net.SendDataHandler _tsapiHandler;

		public override void Initialize()
		{
			ServerApi.Hooks.GameInitialize.Register(this, OnInit);
			ServerApi.Hooks.GamePostInitialize.Register(this, OnPostInit);
			ServerApi.Hooks.GameUpdate.Register(this, OnUpdate, 9000);

			RegionHooks.RegionDeleted += OnRegionDeleted;

			_tsapiHandler = OTAPI.Hooks.Net.SendData;
			OTAPI.Hooks.Net.SendData = OnOtapiSendData;
		}

		protected override void Dispose(bool disposing)
		{
			if (disposing)
			{
				ServerApi.Hooks.GameInitialize.Deregister(this, OnInit);
				ServerApi.Hooks.GamePostInitialize.Deregister(this, OnPostInit);

				RegionHooks.RegionDeleted -= OnRegionDeleted;

				OTAPI.Hooks.Net.SendData = _tsapiHandler;
			}
			base.Dispose(disposing);
		}

		private DateTime _lastCheck = DateTime.UtcNow;

		private void OnUpdate(EventArgs args)
		{
			if ((DateTime.UtcNow - _lastCheck).TotalSeconds < DefaultCheckTime)
			{
				return;
			}

			foreach (var player in TShock.Players.Where(p => p?.Active == true))
			{
				var info = PlayerInfo.GetPlayerInfo(player);
				var oldRegion = info.CurrentRegion;
				info.CurrentRegion = TShock.Regions.GetTopRegion(TShock.Regions.InAreaRegion(player.TileX, player.TileY));

				if (oldRegion == info.CurrentRegion)
					continue;

				var shouldInvokeLeave = true;

				// 若是pz区域，则更换模式；不需要在离开区域时再次复原或保存备份。
				if (info.CurrentRegion != null)
				{
					var region = PzRegions.GetRegionById(info.CurrentRegion.ID);

					if (region != null)
					{
						if (!info.BypassChange)
						{
							info.FakeSscStatus = true;

							if (!info.InSscRegion)
							{
								info.InSscRegion = true;
								info.SetBackupPlayerData();
							}

							if (region.HasDefault)
								info.ChangeCharacter(region.GetDefaultData());

							shouldInvokeLeave = false;
						}
					}
				}

				// 如果从区域出去，且没有进入新pz区域，则恢复
				if (shouldInvokeLeave && oldRegion != null)
				{
					if (!info.InSscRegion || info.FakeSscStatus == DefaultFakeSscStatus)
						continue;

					info.RestoreCharacter();

					info.InSscRegion = false;
					info.FakeSscStatus = false;
				}
			}

			_lastCheck = DateTime.UtcNow;
		}

		private HookResult OnOtapiSendData(ref int bufferId, ref int msgType, ref int remoteClient, ref int ignoreClient, ref NetworkText text, ref int number, ref float number2, ref float number3, ref float number4, ref int number5, ref int number6, ref int number7)
		{
			if (msgType != (int)PacketTypes.WorldInfo)
			{
				return _tsapiHandler(ref bufferId, ref msgType, ref remoteClient, ref ignoreClient, ref text, ref number, ref number2, ref number3, ref number4, ref number5, ref number6, ref number7);
			}

			if (remoteClient == -1)
			{
				var onData = PackInfo(true);
				var offData = PackInfo(false);

				foreach (var tsPlayer in TShock.Players.Where(p => p?.Active == true))
				{
					if (!SendDataHooks.InvokePreSendData(remoteClient, tsPlayer.Index)) continue;
					try
					{
						tsPlayer.SendRawData(PlayerInfo.GetPlayerInfo(tsPlayer).FakeSscStatus ?? DefaultFakeSscStatus ? onData : offData);
					}
					catch
					{
						// ignored
					}
					SendDataHooks.InvokePostSendData(remoteClient, tsPlayer.Index);
				}
			}
			else
			{
				var player = TShock.Players.ElementAtOrDefault(remoteClient);

				if (player != null)
				{
					var info = PlayerInfo.GetPlayerInfo(player);

					/* 如果在区域内，收到了来自别的插件的发送请求
					 * 保持默认 ssc = true 并发送(也就是不需要改什么)
					 * 如果在区域外，收到了来自别的插件的发送请求
					 * 需要 fake ssc = false 并发送
					 */
					SendInfo(remoteClient, info.FakeSscStatus ?? DefaultFakeSscStatus);
				}
			}

			return HookResult.Cancel;
		}

		private void OnPostInit(EventArgs args)
		{
			PzRegions.ReloadRegions();
		}

		private void OnInit(EventArgs args)
		{
			if (!TShock.ServerSideCharacterConfig.Enabled)
			{
				TShock.Log.ConsoleError("[Pz] 未开启SSC! 你可能选错了插件.");
				Dispose(true);
				throw new NotSupportedException("该插件不支持非SSC模式运行!");
			}

			Commands.ChatCommands.Add(new Command("pz.admin.manage", PzCmd, "pz") { AllowServer = false });
			Commands.ChatCommands.Add(new Command("pz.admin.toggle", ToggleBypass, "pztoggle") { AllowServer = false });
			Commands.ChatCommands.Add(new Command("pz.select", PzSelect, "pzselect") { AllowServer = false });

			PzRegions = new PzRegionManager(TShock.DB);
		}

		private void OnRegionDeleted(RegionHooks.RegionDeletedEventArgs args)
		{
			if (!PzRegions.PzRegions.Exists(p => p.Id == args.Region.ID))
				return;

			PzRegions.RemoveRegion(args.Region.ID);
		}

		private static void ToggleBypass(CommandArgs args)
		{
			var info = PlayerInfo.GetPlayerInfo(args.Player);

			info.BypassChange = !info.BypassChange;

			args.Player.SendSuccessMessage("{0}调整跳过装备更换模式。", info.BypassChange ? "关闭" : "开启");
		}

		private void PzSelect(CommandArgs args)
		{
			if (args.Parameters.Count == 0)
			{
				args.Player.SendErrorMessage("参数错误！正确用法：/pzselect <存档名>");
			}

			if (args.Player.CurrentRegion == null)
			{
				args.Player.SendInfoMessage("区域无效。");
				return;
			}

			var region = PzRegions.GetRegionById(args.Player.CurrentRegion.ID);
			if (region == null)
			{
				args.Player.SendInfoMessage("区域无效。");
				return;
			}

			var name = string.Join(" ", args.Parameters);

			if (!region.PlayerDatas.TryGetValue(name, out PlayerData data))
			{
				args.Player.SendInfoMessage("未找到对应存档名。");
				return;
			}

			PlayerInfo.GetPlayerInfo(args.Player).ChangeCharacter(data);
			args.Player.SendInfoMessage("你的人物存档被切换为{0}。", name);
		}

		private void PzCmd(CommandArgs args)
		{
			var cmd = args.Parameters.Count == 0 ? "HELP" : args.Parameters[0].ToUpperInvariant();

			switch (cmd)
			{
				case "ADD":
					#region add
					if (args.Parameters.Count < 3)
					{
						args.Player.SendErrorMessage("语法无效! 正确语法: /pz add <区域名> <存档名> [玩家名]");
						return;
					}

					var regionName = args.Parameters[1];
					var name = args.Parameters[2];
					var playerName = args.Parameters.ElementAtOrDefault(3);

					if (name.Length > 10)
					{
						args.Player.SendErrorMessage("存档名的长度不能超过10!");
						return;
					}

					var region = TShock.Regions.GetRegionByName(regionName);
					if (region == null)
					{
						args.Player.SendErrorMessage("区域名无效!");
						return;
					}
					TSPlayer player = null;
					if (!string.IsNullOrWhiteSpace(playerName))
					{
						var players = TSPlayer.FindByNameOrID(playerName);
						if (players.Count == 0)
						{
							args.Player.SendErrorMessage("未找到玩家!");
							return;
						}
						if (players.Count > 1)
						{
						    args.Player.SendMultipleMatchError(players.Select(p => p.Name));
							return;
						}
						player = players[0];
					}
					player = player ?? args.Player;
					var data = new PlayerData(null);
					data.CopyCharacter(player);

					PzRegions.AddRegion(region.ID);
					PzRegions.AddCharacter(region.ID, name, data);
					args.Player.SendSuccessMessage("添加区域完毕.");
					#endregion
					break;
				case "LIST":
					#region list
					int pageNumber;
					if (!PaginationTools.TryParsePageNumber(args.Parameters, 1, args.Player, out pageNumber))
						return;
					var names = from pz in PzRegions.PzRegions
								select TShock.Regions.GetRegionByID(pz.Id).Name + ": " + string.Join(", ", pz.PlayerDatas.Keys);
					PaginationTools.SendPage(args.Player, pageNumber, PaginationTools.BuildLinesFromTerms(names),
						new PaginationTools.Settings
						{
							HeaderFormat = "应用区域 ({0}/{1}):",
							FooterFormat = "键入 {0}pz list {{0}} 以获取下一页应用区域.".SFormat(Commands.Specifier),
							NothingToDisplayString = "当前没有可用应用区域."
						});
					#endregion
					break;
				case "REMOVE":
					#region remove
					if (args.Parameters.Count == 1)
					{
						args.Player.SendErrorMessage("语法无效! 正确语法: /pz remove <区域名>");
						return;
					}
					regionName = string.Join(" ", args.Parameters.Skip(1));
					region = TShock.Regions.GetRegionByName(regionName);
					if (region == null)
					{
						args.Player.SendErrorMessage("区域名无效!");
						return;
					}

					PzRegions.RemoveRegion(region.ID);
					args.Player.SendSuccessMessage("删除区域及存档完毕.");
					#endregion
					break;
				case "REMOVECHAR":
					#region removeChar
					if (args.Parameters.Count < 3)
					{
						args.Player.SendErrorMessage("语法无效! 正确语法: /pz removechar <区域名> <存档名>");
						return;
					}
					regionName = args.Parameters[1];
					name = args.Parameters[2];
					region = TShock.Regions.GetRegionByName(regionName);
					if (region == null)
					{
						args.Player.SendErrorMessage("区域名无效!");
						return;
					}

					PzRegions.RemoveCharacter(region.ID, name);
					args.Player.SendSuccessMessage("删除存档完毕.");
					#endregion
					break;
				case "DEFAULT":
					#region default
					if (args.Parameters.Count < 3)
					{
						args.Player.SendErrorMessage("语法无效! 正确语法: /pz default <区域名> <存档名>");
						return;
					}
					regionName = args.Parameters[1];
					name = args.Parameters[2];
					region = TShock.Regions.GetRegionByName(regionName);
					if (region == null)
					{
						args.Player.SendErrorMessage("区域名无效!");
						return;
					}

					var pzregion = PzRegions.GetRegionById(region.ID);
					if (pzregion == null)
					{
						args.Player.SendErrorMessage("该区域并卟是Pz区域!");
						return;
					}
					if (!pzregion.PlayerDatas.ContainsKey(name))
					{
						args.Player.SendErrorMessage("区域内未找到符合条件的存档!");
						return;
					}

					PzRegions.SetDefaultCharacter(region.ID, name);
					args.Player.SendSuccessMessage("设定存档完毕.");
					#endregion
					break;
				case "DELDEFAULT":
					#region deldefault
					if (args.Parameters.Count == 1)
					{
						args.Player.SendErrorMessage("语法无效! 正确语法: /pz deldefault <区域名>");
						return;
					}
					regionName = string.Join(" ", args.Parameters.Skip(1));
					region = TShock.Regions.GetRegionByName(regionName);
					if (region == null)
					{
						args.Player.SendErrorMessage("区域名无效!");
						return;
					}

					pzregion = PzRegions.GetRegionById(region.ID);
					if (pzregion == null)
					{
						args.Player.SendErrorMessage("该区域并卟是Pz区域!");
						return;
					}

					PzRegions.SetDefaultCharacter(region.ID, null);
					args.Player.SendSuccessMessage("移除默认存档完毕.");
					#endregion
					break;
				case "SHOW":
				case "RESTORE":
					args.Player.SendErrorMessage("暂不支持该功能.");
					break;
				case "HELP":
					#region help
					if (!PaginationTools.TryParsePageNumber(args.Parameters, 1, args.Player, out pageNumber))
						return;
					var help = new[]
					{
						"add <区域名> <存档名> [玩家名(默认为自己)] - - 增加区域内存档",
						"remove <区域名> - - 删除区域内所有存档",
						"removechar <区域名> <存档名> - - 删除区域内存档",
						"default <区域名> <存档名> - - 设置单一存档默认值",
						"deldefault <区域名> - - 删除单一存档默认值",
						"list [页码] - - 显示所有区域",
						"help [页码] - - 显示子指令帮助"
					};
					PaginationTools.SendPage(args.Player, pageNumber, help,
						new PaginationTools.Settings
						{
							HeaderFormat = "应用区域指令帮助 ({0}/{1}):",
							FooterFormat = "键入 {0}pz help {{0}} 以获取下一页应用区域帮助.".SFormat(Commands.Specifier),
							NothingToDisplayString = "当前没有可用帮助."
						});
					#endregion
					break;
				default:
					args.Player.SendErrorMessage("语法无效! 键入 /pz help 以获取帮助.");
					return;
			}
		}

		private static byte[] PackInfo(bool ssc)
		{
			var memoryStream = new MemoryStream();
			var binaryWriter = new BinaryWriter(memoryStream);
			var position = binaryWriter.BaseStream.Position;
			binaryWriter.BaseStream.Position += 2L;
			binaryWriter.Write((byte)PacketTypes.WorldInfo);

			binaryWriter.Write((int)Main.time);
			BitsByte bb3 = 0;
			bb3[0] = Main.dayTime;
			bb3[1] = Main.bloodMoon;
			bb3[2] = Main.eclipse;
			binaryWriter.Write(bb3);
			binaryWriter.Write((byte)Main.moonPhase);
			binaryWriter.Write((short)Main.maxTilesX);
			binaryWriter.Write((short)Main.maxTilesY);
			binaryWriter.Write((short)Main.spawnTileX);
			binaryWriter.Write((short)Main.spawnTileY);
			binaryWriter.Write((short)Main.worldSurface);
			binaryWriter.Write((short)Main.rockLayer);
			binaryWriter.Write(Main.worldID);
			binaryWriter.Write(Main.worldName);
			binaryWriter.Write(Main.ActiveWorldFileData.UniqueId.ToByteArray());
			binaryWriter.Write(Main.ActiveWorldFileData.WorldGeneratorVersion);
			binaryWriter.Write((byte)Main.moonType);
			binaryWriter.Write((byte)WorldGen.treeb);
			binaryWriter.Write((byte)WorldGen.corruptBG);
			binaryWriter.Write((byte)WorldGen.jungleBG);
			binaryWriter.Write((byte)WorldGen.snowBG);
			binaryWriter.Write((byte)WorldGen.hallowBG);
			binaryWriter.Write((byte)WorldGen.crimsonBG);
			binaryWriter.Write((byte)WorldGen.desertBG);
			binaryWriter.Write((byte)WorldGen.oceanBG);
			binaryWriter.Write((byte)Main.iceBackStyle);
			binaryWriter.Write((byte)Main.jungleBackStyle);
			binaryWriter.Write((byte)Main.hellBackStyle);
			binaryWriter.Write(Main.windSpeedSet);
			binaryWriter.Write((byte)Main.numClouds);
			for (var k = 0; k < 3; k++)
			{
				binaryWriter.Write(Main.treeX[k]);
			}
			for (var l = 0; l < 4; l++)
			{
				binaryWriter.Write((byte)Main.treeStyle[l]);
			}
			for (var m = 0; m < 3; m++)
			{
				binaryWriter.Write(Main.caveBackX[m]);
			}
			for (var n = 0; n < 4; n++)
			{
				binaryWriter.Write((byte)Main.caveBackStyle[n]);
			}
			if (!Main.raining)
			{
				Main.maxRaining = 0f;
			}
			binaryWriter.Write(Main.maxRaining);
			BitsByte bb4 = 0;
			bb4[0] = WorldGen.shadowOrbSmashed;
			bb4[1] = NPC.downedBoss1;
			bb4[2] = NPC.downedBoss2;
			bb4[3] = NPC.downedBoss3;
			bb4[4] = Main.hardMode;
			bb4[5] = NPC.downedClown;
			bb4[6] = ssc;
			bb4[7] = NPC.downedPlantBoss;
			binaryWriter.Write(bb4);
			BitsByte bb5 = 0;
			bb5[0] = NPC.downedMechBoss1;
			bb5[1] = NPC.downedMechBoss2;
			bb5[2] = NPC.downedMechBoss3;
			bb5[3] = NPC.downedMechBossAny;
			bb5[4] = Main.cloudBGActive >= 1f;
			bb5[5] = WorldGen.crimson;
			bb5[6] = Main.pumpkinMoon;
			bb5[7] = Main.snowMoon;
			binaryWriter.Write(bb5);
			BitsByte bb6 = 0;
			bb6[0] = Main.expertMode;
			bb6[1] = Main.fastForwardTime;
			bb6[2] = Main.slimeRain;
			bb6[3] = NPC.downedSlimeKing;
			bb6[4] = NPC.downedQueenBee;
			bb6[5] = NPC.downedFishron;
			bb6[6] = NPC.downedMartians;
			bb6[7] = NPC.downedAncientCultist;
			binaryWriter.Write(bb6);
			BitsByte bb7 = 0;
			bb7[0] = NPC.downedMoonlord;
			bb7[1] = NPC.downedHalloweenKing;
			bb7[2] = NPC.downedHalloweenTree;
			bb7[3] = NPC.downedChristmasIceQueen;
			bb7[4] = NPC.downedChristmasSantank;
			bb7[5] = NPC.downedChristmasTree;
			bb7[6] = NPC.downedGolemBoss;
			bb7[7] = BirthdayParty.PartyIsUp;
			binaryWriter.Write(bb7);
			BitsByte bb8 = 0;
			bb8[0] = NPC.downedPirates;
			bb8[1] = NPC.downedFrost;
			bb8[2] = NPC.downedGoblins;
			bb8[3] = Sandstorm.Happening;
			bb8[4] = DD2Event.Ongoing;
			bb8[5] = DD2Event.DownedInvasionT1;
			bb8[6] = DD2Event.DownedInvasionT2;
			bb8[7] = DD2Event.DownedInvasionT3;
			binaryWriter.Write(bb8);
			binaryWriter.Write((sbyte)Main.invasionType);
			binaryWriter.Write(SocialAPI.Network != null ? SocialAPI.Network.GetLobbyId() : 0UL);
			binaryWriter.Write(Sandstorm.IntendedSeverity);

			var currentPosition = (int)binaryWriter.BaseStream.Position;
			binaryWriter.BaseStream.Position = position;
			binaryWriter.Write((short)currentPosition);
			binaryWriter.BaseStream.Position = currentPosition;
			var data = memoryStream.ToArray();

			binaryWriter.Close();

			return data;
		}

		internal static void SendInfo(int remoteClient, bool ssc)
		{
			if (!SendDataHooks.InvokePreSendData(remoteClient, remoteClient))
				return;

			Main.ServerSideCharacter = ssc;

			NetMessage.SendDataDirect((int)PacketTypes.WorldInfo, remoteClient);

			Main.ServerSideCharacter = true;

			SendDataHooks.InvokePostSendData(remoteClient, remoteClient);
		}
	}
}
