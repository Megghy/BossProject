using System.Linq;
using Terraria;
using Terraria.ID;
using TShockAPI;

namespace WorldEdit.Commands
{
	public class FixGrass : WECommand
	{
		private static ushort[] grassTiles = new[]
		{
			TileID.CorruptGrass,
			TileID.CrimsonGrass,
			TileID.Grass,
			TileID.HallowedGrass,
			TileID.JungleGrass,
			TileID.MushroomGrass,
			TileID.GolfGrass,
			TileID.GolfGrassHallowed
		};

		public FixGrass(int x, int y, int x2, int y2, TSPlayer plr)
			: base(x, y, x2, y2, plr)
		{
		}

		public override void Execute()
        {
            if (!CanUseCommand()) { return; }
            Tools.PrepareUndo(x, y, x2, y2, plr);
			int edits = 0;
			for (int i = x; i <= x2; i++)
			{
				for (int j = y; j <= y2; j++)
				{
					ushort type = Main.tile[i, j].type;
					if (grassTiles.Contains(type))
					{
						if (TileSolid(i - 1, j - 1) && TileSolid(i - 1, j) && TileSolid(i - 1, j + 1) && TileSolid(i, j - 1)
							&& TileSolid(i, j + 1) && TileSolid(i + 1, j) && TileSolid(i + 1, j) && TileSolid(i + 1, j + 1))
						{
							Main.tile[i, j].type = (ushort)(type == TileID.JungleGrass || type == TileID.MushroomGrass ? 59 : 0);
							edits++;
						}
					}
				}
			}
			ResetSection();
			plr.SendSuccessMessage("Fixed grass. ({0})", edits);
		}
	}
}