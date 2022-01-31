using Terraria;
using TShockAPI;
using WorldEdit.Expressions;

namespace WorldEdit.Commands
{
    public class Inactive : WECommand
    {
        private Expression expression;
        private int inactiveType;

        public Inactive(int x, int y, int x2, int y2, MagicWand magicWand, TSPlayer plr, int inacType, Expression expression)
            : base(x, y, x2, y2, magicWand, plr)
        {
            this.inactiveType = inacType;
            this.expression = expression ?? new TestExpression(new Test(t => true));
        }

        public override void Execute()
        {
            if (!CanUseCommand()) { return; }
            Tools.PrepareUndo(x, y, x2, y2, plr);
            int edits = 0;
            switch (inactiveType)
            {
                case 0:
                    for (int i = x; i <= x2; i++)
                    {
                        for (int j = y; j <= y2; j++)
                        {
                            var tile = Main.tile[i, j];
                            if (tile.active() && !tile.inActive() && select(i, j, plr) && expression.Evaluate(tile) && magicWand.InSelection(i, j))
                            {
                                tile.inActive(true);
                                edits++;
                            }
                        }
                    }
                    ResetSection();
                    plr.SendSuccessMessage("Made tiles inactive. ({0})", edits);
                    break;
                case 1:
                    for (int i = x; i <= x2; i++)
                    {
                        for (int j = y; j <= y2; j++)
                        {
                            var tile = Main.tile[i, j];
                            if (tile.inActive() && select(i, j, plr) && expression.Evaluate(tile))
                            {
                                tile.inActive(false);
                                edits++;
                            }
                        }
                    }
                    ResetSection();
                    plr.SendSuccessMessage("Set	tiles' inactive	status off.	({0})", edits);
                    break;
                case 2:
                    for (int i = x; i <= x2; i++)
                    {
                        for (int j = y; j <= y2; j++)
                        {
                            var tile = Main.tile[i, j];
                            if (tile.active() && select(i, j, plr) && expression.Evaluate(tile))
                            {
                                tile.inActive(!tile.inActive());
                                edits++;
                            }
                        }
                    }
                    ResetSection();
                    plr.SendSuccessMessage("Reversed tiles'	inactive status. ({0})", edits);
                    break;
            }
        }
    }
}