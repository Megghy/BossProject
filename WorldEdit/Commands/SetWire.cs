using Terraria;
using TShockAPI;
using WorldEdit.Expressions;

namespace WorldEdit.Commands
{
	public class SetWire : WECommand
	{
		private Expression expression;
		private bool state;
		private int wire;

		public SetWire(int x, int y, int x2, int y2, MagicWand magicWand, TSPlayer plr, int wire, bool state, Expression expression)
			: base(x, y, x2, y2, magicWand, plr)
		{
			this.expression = expression ?? new TestExpression(new Test(t => true));
			this.state = state;
			this.wire = wire;
		}

		public override void Execute()
        {
            if (!CanUseCommand()) { return; }
            Tools.PrepareUndo(x, y, x2, y2, plr);
			int edits = 0;
			switch (wire)
			{
				case 1:
					for (int i = x; i <= x2; i++)
					{
						for (int j = y; j <= y2; j++)
						{
							var tile = Main.tile[i, j];
							if (tile.wire() != state && select(i, j, plr) && expression.Evaluate(tile) && magicWand.InSelection(i, j))
							{
								tile.wire(state);
								edits++;
							}
						}
					}
					ResetSection();
					plr.SendSuccessMessage("Set wire. ({0})", edits);
					return;
				case 2:
					for (int i = x; i <= x2; i++)
					{
						for (int j = y; j <= y2; j++)
						{
							var tile = Main.tile[i, j];
							if (tile.wire2() != state && select(i, j, plr) && expression.Evaluate(tile))
							{
								tile.wire2(state);
								edits++;
							}
						}
					}
					ResetSection();
					plr.SendSuccessMessage("Set wire 2. ({0})", edits);
					return;
				case 3:
					for (int i = x; i <= x2; i++)
					{
						for (int j = y; j <= y2; j++)
						{
							var tile = Main.tile[i, j];
							if (tile.wire3() != state && select(i, j, plr) && expression.Evaluate(tile))
							{
								tile.wire3(state);
								edits++;
							}
						}
					}
					ResetSection();
					plr.SendSuccessMessage("Set wire 3. ({0})", edits);
					return;
				case 4:
					for (int i = x; i <= x2; i++)
					{
						for (int j = y; j <= y2; j++)
						{
							var tile = Main.tile[i, j];
							if (tile.wire4() != state && select(i, j, plr) && expression.Evaluate(tile))
							{
								tile.wire4(state);
								edits++;
							}
						}
					}
					ResetSection();
					plr.SendSuccessMessage("Set wire 4. ({0})", edits);
					return;
			}
		}
	}
}
