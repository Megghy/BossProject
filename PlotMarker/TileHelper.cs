using System.Linq;
using Terraria;

namespace PlotMarker
{
	internal static class TileHelper
	{
		public static void ResetSection(int x, int y, int width, int height)
		{
			var lowX = Netplay.GetSectionX(x);
			var highX = Netplay.GetSectionX(x + width);
			var lowY = Netplay.GetSectionY(y);
			var highY = Netplay.GetSectionY(y + height);

			foreach (var sock in Netplay.Clients.Where(s => s.IsActive))
			{
				for (var i = lowX; i <= highX; i++)
				{
					for (var j = lowY; j <= highY; j++)
						sock.TileSections[i, j] = false;
				}
			}
		}

		public static void SetTile(int i, int j, int tileType, int color = 0)
		{
			var tile = Main.tile[i, j];
			switch (tileType)
			{
				case -1:
					tile.active(false);
					tile.frameX = -1;
					tile.frameY = -1;
					tile.liquidType(0);
					tile.liquid = 0;
					tile.type = 0;
					return;
				case -2:
					tile.active(false);
					tile.liquidType(1);
					tile.liquid = 255;
					tile.type = 0;
					return;
				case -3:
					tile.active(false);
					tile.liquidType(2);
					tile.liquid = 255;
					tile.type = 0;
					return;
				case -4:
					tile.active(false);
					tile.liquidType(0);
					tile.liquid = 255;
					tile.type = 0;
					return;
				default:
					if (Main.tileFrameImportant[tileType])
						WorldGen.PlaceTile(i, j, tileType);
					else
					{
						tile.active(true);
						tile.frameX = -1;
						tile.frameY = -1;
						tile.liquidType(0);
						tile.liquid = 0;
						tile.slope(0);
						tile.type = (ushort)tileType;
					}
					tile.color((byte)color);
					return;
			}
		}

		public static void SetWall(int i, int j, int wallType, int color = 0)
		{
			var tile = Main.tile[i, j];
			if (tile.wall != wallType)
			{
				tile.wall = (byte)wallType;
			}
			tile.wallColor((byte)color);
		}

		public static void RemoveTiles(int x, int y, int w, int h)
		{
			for (var i = x; i < x + w; i++)
			{
				for (var j = y; j < y + h; j++)
				{
					Main.tile[i, j] = new Tile();
				}
			}
		}

		public static T[] To1D<T>(this T[,] data)
			=> data.OfType<T>().ToArray();
		public static T[,] To2D<T>(this T[] data, int width, int height)
        {
			if (data.Length != width * height)
				throw new ArgumentException("wrong size");
			var result = new T[width, height];
			var index = 0;
			for (var i = 0; i < width; i++)
            {
				for(var j = 0; j < height; j++)
                {
					result[i, j] = data[index];
					index++;
				}
            }
			return result;
        }
	}
}
