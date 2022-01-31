using Terraria;
using TShockAPI;

namespace WorldEdit.Commands
{
	public class Flood : WECommand
	{
		private int liquid;

		public Flood(int x, int y, int x2, int y2, MagicWand magicWand, TSPlayer plr, int liquid)
			: base(x, y, x2, y2, magicWand, plr)
		{
			this.liquid = liquid;
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
					if ((!tile.active() || !Main.tileSolid[tile.type]) && magicWand.InSelection(i, j))
					{
						tile.liquidType((byte)liquid);
						tile.liquid = 255;
						edits++;
					}
				}
			}
			ResetSection();
			plr.SendSuccessMessage("Flooded area. ({0})", edits);
		}
	}
}