using OTAPI.Tile;
using Terraria;
using TShockAPI;
using WorldEdit.Expressions;

namespace WorldEdit.Commands
{
    public class Replace : WECommand
    {
        private Expression expression;
        private int from;
        private int to;

        public Replace(int x, int y, int x2, int y2, TSPlayer plr, int from, int to, Expression expression)
            : base(x, y, x2, y2, plr)
        {
            this.from = from;
            this.to = to;
            this.expression = expression ?? new TestExpression(new Test(t => true));
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
                    ITile tile = Main.tile[i, j];
                    if ((((from >= 0) && tile.active() && (from == tile.type))
                     || ((from == -1) && !tile.active())
                     || ((from == -2) && (tile.liquid != 0) && (tile.liquidType() == 1))
                     || ((from == -3) && (tile.liquid != 0) && (tile.liquidType() == 2))
                     || ((from == -4) && (tile.liquid == 0) && (tile.liquidType() == 0)))
                     && Tools.CanSet(true, tile, to, select, expression, magicWand, i, j, plr))
                    {
                        SetTile(i, j, to);
                        edits++;
                    }
                }
            }
            ResetSection();
            plr.SendSuccessMessage("Replaced tiles. ({0})", edits);
        }
    }
}