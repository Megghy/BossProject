using Terraria;
using TShockAPI;

namespace WorldEdit.Commands
{
	public class Drain : WECommand
	{
		public Drain(int x, int y, int x2, int y2, TSPlayer plr)
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
					if (tile.liquid != 0)
					{
						tile.liquid = 0;
						tile.liquidType(0);
						edits++;
					}
				}
			}
			ResetSection();
			plr.SendSuccessMessage("Drained area. ({0})", edits);
		}
	}
}
