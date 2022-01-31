using System.Linq;
using Terraria;
using TShockAPI;
using WorldEdit.Expressions;

namespace WorldEdit.Commands
{
	public class SetGrass : WECommand
	{
		private Expression expression;
		private string grass;

		public SetGrass(int x, int y, int x2, int y2, MagicWand magicWand, TSPlayer plr, string grass, Expression expression)
			: base(x, y, x2, y2, magicWand, plr)
		{
			this.expression = expression ?? new TestExpression(t => true);
			this.grass = grass;
		}

		public override void Execute()
		{
			if (x < 1) x = 1;
			else if (x > (Main.maxTilesX - 2)) x = (Main.maxTilesX - 2);
			if (y < 1) y = 1;
			else if (y > (Main.maxTilesY - 2)) y = (Main.maxTilesY - 2);
			if (x2 < 1) x2 = 1;
			else if (x2 > (Main.maxTilesX - 2)) x2 = (Main.maxTilesX - 2);
			if (y2 < 1) y2 = 1;
			else if (y2 > (Main.maxTilesY - 2)) y2 = (Main.maxTilesY - 2);

            if (!CanUseCommand()) { return; }
            Tools.PrepareUndo(x, y, x2, y2, plr);

			var tiles = WorldEdit.Biomes[grass];
			ushort dirtType = (ushort)tiles.Dirt, grassType = (ushort)tiles.Grass.First();

			int edits = 0;
			for (int i = x; i <= x2; i++)
			{
				for (int j = y; j <= y2; j++)
				{
					bool XY = Main.tile[i, j].active();
					bool mXmY = Main.tile[i - 1, j - 1].active();
					bool mXpY = Main.tile[i - 1, j + 1].active();
					bool pXmY = Main.tile[i + 1, j - 1].active();
					bool pXpY = Main.tile[i + 1, j + 1].active();
					bool mXY = Main.tile[i - 1, j].active();
					bool pXY = Main.tile[i + 1, j].active();
					bool XmY = Main.tile[i, j - 1].active();
					bool XpY = Main.tile[i, j + 1].active();

					if (XY && !(mXmY && mXpY && pXmY && pXpY && mXY && pXY && XmY && XpY)
						&& expression.Evaluate(Main.tile[i, j])
						&& Main.tile[i, j].type == dirtType && magicWand.InSelection(i, j))
					{
						Main.tile[i, j].type = grassType;
						edits++;
					}
				}
			}
			ResetSection();
			plr.SendSuccessMessage("Set {1} grass. ({0})", edits, grass);
		}
	}
}