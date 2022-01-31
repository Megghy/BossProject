using System.Collections.Generic;
using System.Linq;
using Terraria;
using TShockAPI;
using WorldEdit.Expressions;

namespace WorldEdit
{
    public class MagicWand
    {
        public static int MaxPointCount;

        internal bool dontCheck = false;
        internal List<WEPoint> Points = new List<WEPoint>();
        public MagicWand() => dontCheck = true;
        public MagicWand(WEPoint[] Points)
        {
            this.dontCheck = false;
            this.Points = Points?.ToList() ?? new List<WEPoint>();
        }
        public bool InSelection(int X, int Y) =>
            dontCheck ? true : Points.Any(p => p.X == X && p.Y == Y);
        
        public static bool GetMagicWandSelection(int X, int Y, Expression Expression,
            TSPlayer Player, out MagicWand MagicWand)
        {
            MagicWand = new MagicWand();
            if (!Tools.InMapBoundaries(X, Y) || (Expression == null))
            { return false; }
            if (!Expression.Evaluate(Main.tile[X, Y]))
            { return false; }
            short x = (short)X, y = (short)Y;
            List<WEPoint> WEPoints = new List<WEPoint>() { new WEPoint(x, y) };
            int index = 0, count = 0;
            bool[,] was = new bool[Main.maxTilesX, Main.maxTilesY];
            was[x, y] = true;
            while (index < WEPoints.Count)
            {
                WEPoint point = WEPoints[index];
                foreach (WEPoint p in new WEPoint[]
                {
                    new WEPoint((short)(point.X + 1), point.Y),
                    new WEPoint((short)(point.X - 1), point.Y),
                    new WEPoint(point.X, (short)(point.Y + 1)),
                    new WEPoint(point.X, (short)(point.Y - 1))
                })
                {
                    if (Tools.InMapBoundaries(p.X, p.Y) && !was[p.X, p.Y])
                    {
                        if (Expression.Evaluate(Main.tile[p.X, p.Y]))
                        {
                            WEPoints.Add(p);
                            count++;
                        }
                        was[p.X, p.Y] = true;
                    }
                }
                if (count >= MaxPointCount)
                {
                    Player.SendErrorMessage("Hard selection tile limit " +
                        $"({MaxPointCount}) has been reached.");
                    return false;
                }
                index++;
            }

            MagicWand = new MagicWand(WEPoints.ToArray());
            return true;
        }
    }

    public struct WEPoint
    {
        public short X { get; set; }
        public short Y { get; set; }
        public WEPoint(short X, short Y)
        {
            this.X = X;
            this.Y = Y;
        }
    }
}