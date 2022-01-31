using Terraria;
using TShockAPI;

namespace WorldEdit.Commands
{
	public class FixSlopes : WECommand
	{
		public FixSlopes(int x, int y, int x2, int y2, TSPlayer plr)
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
					int slope = Main.tile[i, j].slope();
					if (((slope == 1 || slope == 2) && TileSolid(i, j - 1)) ||
						((slope == 3 || slope == 4) && TileSolid(i, j + 1)))
					{
						Main.tile[i, j].slope(0);
						edits++;
					}
				}
			}
			ResetSection();
			plr.SendSuccessMessage("Fixed slopes. ({0})", edits);
		}
	}
}
