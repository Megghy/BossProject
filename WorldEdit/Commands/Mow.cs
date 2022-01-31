using System.Linq;
using Terraria;
using Terraria.ID;
using TShockAPI;

namespace WorldEdit.Commands
{
	public class Mow : WECommand
	{
		private static ushort[] mowedTiles = new[]
		{
			TileID.DyePlants,
			TileID.Plants,
			TileID.Plants2,
			TileID.CorruptPlants,
			TileID.HallowedPlants,
			TileID.HallowedPlants2,
			TileID.JunglePlants,
			TileID.JunglePlants2,
			TileID.MushroomPlants,
			TileID.CrimsonPlants,
			TileID.CorruptThorns,
			TileID.CrimsonThorns,
			TileID.JungleThorns,
			TileID.Vines,
			TileID.CrimsonVines,
			TileID.HallowedVines,
			TileID.JungleVines
		};

		public Mow(int x, int y, int x2, int y2, TSPlayer plr)
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
					var tile = Main.tile[i, j];
					if (mowedTiles.Contains(tile.type))
					{
						tile.active(false);
						tile.type = 0;
						edits++;
					}
				}
			}
			ResetSection();
			plr.SendSuccessMessage("Mowed grass, thorns, and vines. ({0})", edits);
		}
	}
}
