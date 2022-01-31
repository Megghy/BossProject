using Terraria;
using TShockAPI;
using WorldEdit.Expressions;

namespace WorldEdit.Commands
{
	public class Smooth : WECommand
	{
		private Expression expression;

		public Smooth(int x, int y, int x2, int y2, MagicWand magicWand, TSPlayer plr, Expression expression)
			: base(x, y, x2, y2, magicWand, plr)
		{
			this.expression = expression ?? new TestExpression(new Test(t => true));
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
			int edits = 0;
			for (int i = x; i <= x2; i++)
			{
				for (int j = y; j <= y2; j++)
				{
					bool XY = Main.tile[i, j].active();
					bool slope = (Main.tile[i, j].slope() == 0);
					bool mXY = magicWand.dontCheck
                                    ? Main.tile[i - 1, j].active()
                                    : magicWand.InSelection(i - 1, j);
					bool pXY = magicWand.dontCheck
                                    ? Main.tile[i + 1, j].active()
                                    : magicWand.InSelection(i + 1, j);
                    bool XmY = magicWand.dontCheck
                                    ? Main.tile[i, j - 1].active()
                                    : magicWand.InSelection(i, j - 1);
                    bool XpY = magicWand.dontCheck
                                    ? Main.tile[i, j + 1].active()
                                    : magicWand.InSelection(i, j + 1);

                    if (XY && slope && expression.Evaluate(Main.tile[i, j]) && magicWand.InSelection(i, j))
					{
						if (mXY && XmY && !XpY && !pXY)
						{
							Main.tile[i, j].slope(3);
							edits++;
						}
						else if (XmY && pXY && !XpY && !mXY)
						{
							Main.tile[i, j].slope(4);
							edits++;
						}
						else if (pXY && XpY && !mXY && !XmY)
						{
							Main.tile[i, j].slope(2);
							edits++;
						}
						else if (XpY && mXY && !XmY && !pXY)
						{
							Main.tile[i, j].slope(1);
							edits++;
						}
					}
				}
			}
			ResetSection();
			plr.SendSuccessMessage("Smoothed area. ({0})", edits);
		}
	}
}