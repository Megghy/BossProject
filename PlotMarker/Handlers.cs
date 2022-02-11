using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Streams;
using System.Linq;
using Terraria;
using Terraria.ObjectData;
using TShockAPI;
using TShockAPI.Net;
using Microsoft.Xna.Framework;

namespace PlotMarker
{
	internal static class Handlers
	{
		private static readonly Dictionary<PacketTypes, GetDataHandlerDelegate> GetDataHandlerDelegates
			= new(){
				{ PacketTypes.PaintTile, HandlePaintTile },
				{ PacketTypes.PaintWall, HandlePaintWall },
				{ PacketTypes.Tile, HandleTile },
				{ PacketTypes.PlaceObject, HandlePlaceObject },
				{ PacketTypes.MassWireOperation, HandleMassWireOperation },
				{ PacketTypes.LiquidSet, HandleLiquidSet },
				{ PacketTypes.PlaceChest, HandlePlaceChest},
				{ PacketTypes.PlaceTileEntity, HandlePlaceTileEntity},
				{ PacketTypes.TileSendSquare, HandleSendTileSquare}
			};

		public static bool HandleGetData(PacketTypes type, TSPlayer player, MemoryStream data)
		{
			if (GetDataHandlerDelegates.TryGetValue(type, out var handler))
			{
				try
				{
					return handler(new GetDataHandlerArgs(player, data));
				}
				catch (Exception ex)
				{
					TShock.Log.ConsoleError("[PlotMarker] 处理数据内未捕获的异常: 详情查看日志.");
					TShock.Log.Error(ex.ToString());
					return true;
				}
			}
			return false;
		}

		private static bool HandleSendTileSquare(GetDataHandlerArgs args)
		{
			var player = args.Player;
			var size = args.Data.ReadInt16();
			var tileX = args.Data.ReadInt16();
			var tileY = args.Data.ReadInt16();

			var tiles = new NetTile[size, size];
			for (int x = 0; x < size; x++)
			{
				for (int y = 0; y < size; y++)
				{
					tiles[x, y] = new NetTile(args.Data);
				}
			}

			for (int x = 0; x < size; x++)
			{
				int realx = tileX + x;
				if (realx < 0 || realx >= Main.maxTilesX)
					continue;

				for (int y = 0; y < size; y++)
				{
					int realy = tileY + y;
					if (realy < 0 || realy >= Main.maxTilesY)
						continue;

					if (tiles[x, y].Type == Terraria.ID.TileID.LogicSensor && PlotMarker.BlockModify(args.Player, realx, realy))
					{
						args.Player.SendTileSquare(realx, realy, 1);
						return true;
					}
				}
			}

			return false;
		}

		private static bool HandlePlaceTileEntity(GetDataHandlerArgs args)
		{
			var x = args.Data.ReadInt16();
			var y = args.Data.ReadInt16();
			var type = args.Data.ReadByte();

			if (x < 0 || x >= Main.maxTilesX)
				return true;

			if (y < 0 || y >= Main.maxTilesY)
				return true;

			if (PlotMarker.BlockModify(args.Player, x, y))
			{
				args.Player.SendTileSquare(x, y, 3);
				return true;
			}

			return false;
		}

		private static bool HandlePlaceChest(GetDataHandlerArgs args)
		{
			args.Data.ReadByte();
			var x = args.Data.ReadInt16();
			var y = args.Data.ReadInt16();

			if (x < 0 || x >= Main.maxTilesX)
				return true;

			if (y < 0 || y >= Main.maxTilesY)
				return true;

			if (PlotMarker.BlockModify(args.Player, x, y))
			{
				args.Player.SendTileSquare(x, y, 3);
				return true;
			}

			return false;
		}

		private static bool HandlePaintTile(GetDataHandlerArgs args)
		{
			var x = args.Data.ReadInt16();
			var y = args.Data.ReadInt16();
			var t = args.Data.ReadInt8();

			if (x < 0 || y < 0 || x >= Main.maxTilesX || y >= Main.maxTilesY || t > Main.numTileColors)
			{
				return true;
			}

			if (PlotMarker.BlockModify(args.Player, x, y))
			{
				args.Player.SendData(PacketTypes.PaintTile, "", x, y, Main.tile[x, y].color());
				return true;
			}

			return false;
		}

		private static bool HandlePaintWall(GetDataHandlerArgs args)
		{
			var x = args.Data.ReadInt16();
			var y = args.Data.ReadInt16();
			var t = args.Data.ReadInt8();

			if (x < 0 || y < 0 || x >= Main.maxTilesX || y >= Main.maxTilesY || t > Main.numTileColors)
			{
				return true;
			}

			if (PlotMarker.BlockModify(args.Player, x, y))
			{
				args.Player.SendData(PacketTypes.PaintWall, "", x, y, Main.tile[x, y].wallColor());
				return true;
			}

			return false;
		}

		private static bool HandleTile(GetDataHandlerArgs args)
		{
			var info = PlayerInfo.GetInfo(args.Player);
			using (var reader = new BinaryReader(args.Data))
			{
				reader.ReadByte();
				int x = reader.ReadInt16();
				int y = reader.ReadInt16();

				if (info.Status != PlayerInfo.PointStatus.None)
				{
					if (x >= 0 && y >= 0 && x < Main.maxTilesX && y < Main.maxTilesY)
					{
						switch (info.Status)
						{
							case PlayerInfo.PointStatus.Point1:
								info.X = x;
								info.Y = y;
								args.Player.SendInfoMessage("设定点 1 完毕.");
								break;
							case PlayerInfo.PointStatus.Point2:
								info.X2 = x;
								info.Y2 = y;
								args.Player.SendInfoMessage("设定点 2 完毕.");
								break;
							case PlayerInfo.PointStatus.Delegate:
								info.OnGetPoint?.Invoke(x, y, args.Player);
								break;
						}
						info.Status = PlayerInfo.PointStatus.None;
						args.Player.SendTileSquare(x, y, 3);
						return true;
					}
				}

				if (PlotMarker.BlockModify(args.Player, x, y))
				{
					args.Player.SendTileSquare(x, y, 3);
					return true;
				}
			}

			return false;
		}

		private static bool HandlePlaceObject(GetDataHandlerArgs args)
		{
			var x = args.Data.ReadInt16();
			var y = args.Data.ReadInt16();
			var type = args.Data.ReadInt16();
			var style = args.Data.ReadInt16();

			if (type < 0 || type >= Main.maxTileSets)
				return true;

			if (x < 0 || x >= Main.maxTilesX)
				return true;

			if (y < 0 || y >= Main.maxTilesY)
				return true;

			var tileData = TileObjectData.GetTileData(type, style);

			for (int i = x; i < x + tileData.Width; i++)
			{
				for (int j = y; j < y + tileData.Height; j++)
				{
					if (PlotMarker.BlockModify(args.Player, x, y))
					{
						args.Player.SendTileSquare(i, j, 4);
						return true;
					}
				}
			}

			return false;
		}

		private static bool HandleMassWireOperation(GetDataHandlerArgs args)
		{
			var startX = args.Data.ReadInt16();
			var startY = args.Data.ReadInt16();
			var endX = args.Data.ReadInt16();
			var endY = args.Data.ReadInt16();
			args.Data.ReadByte(); // Ignore toolmode

			var data = PlayerInfo.GetInfo(args.Player);
			if (data.Status != PlayerInfo.PointStatus.None)
			{
				if (startX >= 0 && startY >= 0 && endX >= 0 && endY >= 0 && startX < Main.maxTilesX && startY < Main.maxTilesY && endX < Main.maxTilesX && endY < Main.maxTilesY)
				{
					if (startX == endX && startY == endY)
					{
						switch (data.Status)
						{
							case PlayerInfo.PointStatus.Point1:
								data.X = startX;
								data.Y = startY;
								args.Player.SendInfoMessage("设定点 1 完毕.");
								break;
							case PlayerInfo.PointStatus.Point2:
								data.X2 = startX;
								data.Y2 = startY;
								args.Player.SendInfoMessage("设定点 2 完毕.");
								break;
							case PlayerInfo.PointStatus.Delegate:
								data.OnGetPoint?.Invoke(startX, startY, args.Player);
								break;
						}
					}
					else
					{
						switch (data.Status)
						{
							case PlayerInfo.PointStatus.Point1:
							case PlayerInfo.PointStatus.Point2:
								data.X = startX;
								data.Y = startY;
								data.X2 = endX;
								data.Y2 = endY;
								args.Player.SendInfoMessage("设定区域完毕.");
								break;
							case PlayerInfo.PointStatus.Delegate:
								data.OnGetPoint?.Invoke(startX, startY, args.Player);
								break;

						}
					}
					data.Status = PlayerInfo.PointStatus.None;
					return true;
				}
			}

			var points = TShock.Utils.GetMassWireOperationRange(
								new Point(startX, startY),
								new Point(endX, endY),
								args.Player.TPlayer.direction == 1
							);
			return points.Any(p => PlotMarker.BlockModify(args.Player, p.X, p.Y));
		}

		private static bool HandleLiquidSet(GetDataHandlerArgs args)
		{
			int tileX = args.Data.ReadInt16();
			int tileY = args.Data.ReadInt16();

			if (PlotMarker.BlockModify(args.Player, tileX, tileY))
			{
				args.Player.SendTileSquare(tileX, tileY, 4);
				return true;
			}

			return false;
		}
	}
}
