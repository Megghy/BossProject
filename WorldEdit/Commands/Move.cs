using System;
using Terraria;
using TShockAPI;
using WorldEdit.Expressions;
using WorldEdit.Extensions;

namespace WorldEdit.Commands
{
    public class Move : WECommand
    {
        int down;
        int right;
        Expression expression;

        public Move(int x, int y, int x2, int y2, MagicWand magicWand, TSPlayer plr, int down, int right, Expression expression)
            : base(x, y, x2, y2, magicWand, plr)
        {
            this.down = down;
            this.right = right;
            this.expression = expression ?? new TestExpression(new Test(t => true));
        }

        public override void Execute()
        {
            int newX = x + right, newY = y + down;
            if (newX < 0) { newX = 0; }
            if (newY < 0) { newY = 0; }
            if (newX >= Main.maxTilesX - Math.Abs(x - x2))
            { newX = Main.maxTilesX - Math.Abs(x - x2) - 1; }
            if (newY >= Main.maxTilesY - Math.Abs(y - y2))
            { newY = Main.maxTilesY - Math.Abs(y - y2) - 1; }
            int newX2 = newX + Math.Abs(x - x2), newY2 = newY + Math.Abs(y - y2);
            
            int tX = Math.Min(x, Math.Min(newX, newX2));
            int tY = Math.Min(y, Math.Min(newY, newY2));
            int tX2 = Math.Max(x2, Math.Max(newX, newX2));
            int tY2 = Math.Max(y2, Math.Max(newY, newY2));

            if (!CanUseCommand(tX, tY, tX2, tY2)) { return; }
            Tools.PrepareUndo(tX, tY, tX2, tY2, plr);

            WorldSectionData data = Tools.SaveWorldSection(x, y, x2, y2);
            int edits = 0;
            for (int i = x; i <= x2; i++)
            {
                for (int j = y; j <= y2; j++)
                {
                    if (magicWand.InSelection(i, j)
                        && expression.Evaluate(Main.tile[i, j]))
                    {
                        Main.tile[i, j] = new Tile();
                        edits++;
                    }
                }
            }

            for (var i = newX; i <= newX2; i++)
            {
                for (var j = newY; j <= newY2; j++)
                {
                    var index1 = i - newX;
                    var index2 = j - newY;

                    if (i < 0 || j < 0 || i >= Main.maxTilesX || j >= Main.maxTilesY
                        || !magicWand.InSelection(i - right, j - down)
                        || !expression.Evaluate(data.Tiles[index1, index2]))
                    { continue; }

                    Main.tile[i, j] = data.Tiles[index1, index2];
                }
            }

            Tools.LoadWorldSection(data, newX, newY, false);
            ResetSection();

            PlayerInfo info = plr.GetPlayerInfo();
            info.X = newX;
            info.Y = newY;
            info.X2 = newX2;
            info.Y2 = newY2;

            plr.SendInfoMessage("Moved tiles ({0}).", edits);
        }
    }
}