using OTAPI.Tile;
using Terraria;
using TShockAPI;
using WorldEdit.Expressions;

namespace WorldEdit.Commands
{
    public class ReplaceWall : WECommand
    {
        private Expression expression;
        private int from;
        private int to;

        public ReplaceWall(int x, int y, int x2, int y2, TSPlayer plr, int from, int to, Expression expression)
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
                    if ((tile.wall == from)
                     && Tools.CanSet(false, tile, to, select, expression, magicWand, i, j, plr))
                    {
                        tile.wall = (byte)to;
                        edits++;
                    }
                }
            }
            ResetSection();
            plr.SendSuccessMessage("Replaced walls. ({0})", edits);
        }
    }
}