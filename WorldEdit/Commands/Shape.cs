using System;
using System.Linq;
using Terraria;
using TShockAPI;
using WorldEdit.Expressions;

namespace WorldEdit.Commands
{
    public class Shape : WECommand
    {
        private Expression expression;
        private int shapeType;
        private int rotateType;
        private int flipType;
        private bool wall;
        private bool filled;
        private int materialType;

        public Shape(int x, int y, int x2, int y2, MagicWand magicWand, TSPlayer plr,
            int shapeType, int rotateType, int flipType, bool wall, bool filled,
            int materialType, Expression expression)
            : base(x, y, x2, y2, magicWand, plr, false)
        {
            this.expression = expression ?? new TestExpression(new Test(t => true));
            this.shapeType = shapeType;
            this.rotateType = rotateType;
            this.flipType = flipType;
            this.wall = wall;
            this.filled = filled;
            this.materialType = materialType;
        }

        public override void Execute()
        {
            if (!CanUseCommand()) { return; }
            if (shapeType != 0)
            {
                Position(true);
                Tools.PrepareUndo(x, y, x2, y2, plr);
            }
            else
            {
                Tools.PrepareUndo(Math.Min(x, x2), Math.Min(y, y2),
                    Math.Max(x, x2), Math.Max(y, y2), plr);
            }
            int edits = 0;
            switch (shapeType)
            {
                #region Line

                case 0:
                    {
                        WEPoint[] points = Tools.CreateLine(x, y, x2, y2);
                        if (wall)
                        {
                            foreach (WEPoint p in points)
                            {
                                var tile = Main.tile[p.X, p.Y];
                                if (Tools.CanSet(false, Main.tile[p.X, p.Y], materialType,
                                    select, expression, magicWand, p.X, p.Y, plr))
                                {
                                    Main.tile[p.X, p.Y].wall = (ushort)materialType;
                                    edits++;
                                }
                            }
                        }
                        else
                        {
                            foreach (WEPoint p in points)
                            {
                                var tile = Main.tile[p.X, p.Y];
                                if (Tools.CanSet(true, Main.tile[p.X, p.Y], materialType,
                                    select, expression, magicWand, p.X, p.Y, plr))
                                {
                                    SetTile(p.X, p.Y, materialType);
                                    edits++;
                                }
                            }
                        }
                        break;
                    }

                #endregion
                #region Rectangle

                case 1:
                    {
                        if (wall)
                        {
                            for (int i = x; i <= x2; i++)
                            {
                                for (int j = y; j <= y2; j++)
                                {
                                    if (Tools.CanSet(false, Main.tile[i, j], materialType,
                                        select, expression, magicWand, i, j, plr)
                                        && (filled ? true : WorldEdit.Selections["border"](i, j, plr)))
                                    {
                                        Main.tile[i, j].wall = (ushort)materialType;
                                        edits++;
                                    }
                                }
                            }
                        }
                        else
                        {
                            for (int i = x; i <= x2; i++)
                            {
                                for (int j = y; j <= y2; j++)
                                {
                                    if (Tools.CanSet(true, Main.tile[i, j], materialType,
                                        select, expression, magicWand, i, j, plr)
                                        && (filled ? true : WorldEdit.Selections["border"](i, j, plr)))
                                    {
                                        SetTile(i, j, materialType);
                                        edits++;
                                    }
                                }
                            }
                        }
                        break;
                    }

                #endregion
                #region Ellipse

                case 2:
                    {
                        #region Filled

                        if (filled)
                        {
                            if (wall)
                            {
                                for (int i = x; i <= x2; i++)
                                {
                                    for (int j = y; j <= y2; j++)
                                    {
                                        if (Tools.CanSet(false, Main.tile[i, j], materialType,
                                            select, expression, magicWand, i, j, plr)
                                            && WorldEdit.Selections["ellipse"](i, j, plr))
                                        {
                                            Main.tile[i, j].wall = (ushort)materialType;
                                            edits++;
                                        }
                                    }
                                }
                            }
                            else
                            {
                                for (int i = x; i <= x2; i++)
                                {
                                    for (int j = y; j <= y2; j++)
                                    {
                                        if (Tools.CanSet(true, Main.tile[i, j], materialType,
                                            select, expression, magicWand, i, j, plr)
                                            && WorldEdit.Selections["ellipse"](i, j, plr))
                                        {
                                            SetTile(i, j, materialType);
                                            edits++;
                                        }
                                    }
                                }
                            }
                        }

                        #endregion
                        #region NotFilled

                        else
                        {
                            WEPoint[] points = Tools.CreateEllipseOutline(x, y, x2, y2);
                            if (wall)
                            {
                                foreach (WEPoint p in points)
                                {
                                    if (Tools.CanSet(false, Main.tile[p.X, p.Y], materialType,
                                        select, expression, magicWand, p.X, p.Y, plr))
                                    {
                                        Main.tile[p.X, p.Y].wall = (ushort)materialType;
                                        edits++;
                                    }
                                }
                            }
                            else
                            {
                                foreach (WEPoint p in points)
                                {
                                    if (Tools.CanSet(true, Main.tile[p.X, p.Y], materialType,
                                        select, expression, magicWand, p.X, p.Y, plr))
                                    {
                                        SetTile(p.X, p.Y, materialType);
                                        edits++;
                                    }
                                }
                            }
                        }

                        #endregion
                        break;
                    }

                #endregion
                #region IsoscelesTriangle, RightTriangle

                case 3:
                case 4:
                    {
                        WEPoint[] points, line1, line2;
                        if (shapeType == 3)
                        {
                            switch (rotateType)
                            {
                                #region Up

                                case 0:
                                    {
                                        int center = x + ((x2 - x) / 2);
                                        points = Tools.CreateLine(center, y, x, y2)
                                         .Concat(Tools.CreateLine(center + ((x2 - x) % 2), y, x2, y2))
                                         .ToArray();
                                        line1 = new WEPoint[]
                                        {
                                            new WEPoint((short)x, (short)y2),
                                            new WEPoint((short)x2, (short)y2)
                                        };
                                        line2 = null;
                                        break;
                                    }

                                #endregion
                                #region Down

                                case 1:
                                    {
                                        int center = x + ((x2 - x) / 2);
                                        points = Tools.CreateLine(center, y2, x, y)
                                         .Concat(Tools.CreateLine(center + ((x2 - x) % 2), y2, x2, y))
                                         .ToArray();
                                        line1 = new WEPoint[]
                                        {
                                            new WEPoint((short)x, (short)y),
                                            new WEPoint((short)x2, (short)y)
                                        };
                                        line2 = null;
                                        break;
                                    }

                                #endregion
                                #region Left

                                case 2:
                                    {
                                        int center = y + ((y2 - y) / 2);
                                        points = Tools.CreateLine(x, center, x2, y)
                                         .Concat(Tools.CreateLine(x, center + ((y2 - y) % 2), x2, y2))
                                         .ToArray();
                                        line1 = new WEPoint[]
                                        {
                                            new WEPoint((short)x2, (short)y),
                                            new WEPoint((short)x2, (short)y2)
                                        };
                                        line2 = null;
                                        break;
                                    }

                                #endregion
                                #region Right

                                case 3:
                                    {
                                        int center = y + ((y2 - y) / 2);
                                        points = Tools.CreateLine(x2, center, x, y)
                                         .Concat(Tools.CreateLine(x2, center + ((x2 - x) % 2), x, y2))
                                         .ToArray();
                                        line1 = new WEPoint[]
                                        {
                                            new WEPoint((short)x, (short)y),
                                            new WEPoint((short)x, (short)y2)
                                        };
                                        line2 = null;
                                        break;
                                    }

                                #endregion
                                default: return;
                            }
                        }
                        else
                        {
                            switch (flipType)
                            {
                                #region Left

                                case 0:
                                    {
                                        switch (rotateType)
                                        {
                                            #region Up

                                            case 0:
                                                {
                                                    points = Tools.CreateLine(x, y2, x2, y);
                                                    line1 = new WEPoint[]
                                                    {
                                                        new WEPoint((short)x, (short)y2),
                                                        new WEPoint((short)x2, (short)y2)
                                                    };
                                                    line2 = new WEPoint[]
                                                    {
                                                        new WEPoint((short)x2, (short)y),
                                                        new WEPoint((short)x2, (short)y2)
                                                    };
                                                    break;
                                                }

                                            #endregion
                                            #region Down

                                            case 1:
                                                {
                                                    points = Tools.CreateLine(x, y, x2, y2);
                                                    line1 = new WEPoint[]
                                                    {
                                                        new WEPoint((short)x, (short)y),
                                                        new WEPoint((short)x2, (short)y)
                                                    };
                                                    line2 = new WEPoint[]
                                                    {
                                                        new WEPoint((short)x2, (short)y),
                                                        new WEPoint((short)x2, (short)y2)
                                                    };
                                                    break;
                                                }

                                            #endregion
                                            default: return;
                                        }
                                        break;
                                    }

                                #endregion
                                #region Right

                                case 1:
                                    {
                                        switch (rotateType)
                                        {
                                            #region Up

                                            case 0:
                                                {
                                                    points = Tools.CreateLine(x, y, x2, y2);
                                                    line1 = new WEPoint[]
                                                    {
                                                        new WEPoint((short)x, (short)y),
                                                        new WEPoint((short)x, (short)y2)
                                                    };
                                                    line2 = new WEPoint[]
                                                    {
                                                        new WEPoint((short)x, (short)y2),
                                                        new WEPoint((short)x2, (short)y2)
                                                    };
                                                    break;
                                                }

                                            #endregion
                                            #region Down

                                            case 1:
                                                {
                                                    points = Tools.CreateLine(x, y2, x2, y);
                                                    line1 = new WEPoint[]
                                                    {
                                                        new WEPoint((short)x, (short)y),
                                                        new WEPoint((short)x, (short)y2)
                                                    };
                                                    line2 = new WEPoint[]
                                                    {
                                                        new WEPoint((short)x, (short)y),
                                                        new WEPoint((short)x2, (short)y)
                                                    };
                                                    break;
                                                }

                                            #endregion
                                            default: return;
                                        }
                                        break;
                                    }

                                #endregion
                                default: return;
                            }
                        }

                        if (filled)
                        {
                            switch (rotateType)
                            {
                                #region Up

                                case 0:
                                    {
                                        if (wall)
                                        {
                                            foreach (WEPoint p in points)
                                            {
                                                for (int y = p.Y; y <= y2; y++)
                                                {
                                                    var tile = Main.tile[p.X, y];
                                                    if (Tools.CanSet(false, Main.tile[p.X, y], materialType,
                                                        select, expression, magicWand, p.X, y, plr))
                                                    {
                                                        Main.tile[p.X, y].wall = (ushort)materialType;
                                                        edits++;
                                                    }
                                                }
                                            }
                                        }
                                        else
                                        {
                                            foreach (WEPoint p in points)
                                            {
                                                for (int y = p.Y; y <= y2; y++)
                                                {
                                                    var tile = Main.tile[p.X, y];
                                                    if (Tools.CanSet(true, Main.tile[p.X, y], materialType,
                                                        select, expression, magicWand, p.X, y, plr))
                                                    {
                                                        SetTile(p.X, y, materialType);
                                                        edits++;
                                                    }
                                                }
                                            }
                                        }
                                        break;
                                    }

                                #endregion
                                #region Down

                                case 1:
                                    {
                                        if (wall)
                                        {
                                            foreach (WEPoint p in points)
                                            {
                                                for (int y = p.Y; y >= this.y; y--)
                                                {
                                                    var tile = Main.tile[p.X, y];
                                                    if (Tools.CanSet(false, Main.tile[p.X, y], materialType,
                                                        select, expression, magicWand, p.X, y, plr))
                                                    {
                                                        Main.tile[p.X, y].wall = (ushort)materialType;
                                                        edits++;
                                                    }
                                                }
                                            }
                                        }
                                        else
                                        {
                                            foreach (WEPoint p in points)
                                            {
                                                for (int y = p.Y; y >= this.y; y--)
                                                {
                                                    var tile = Main.tile[p.X, y];
                                                    if (Tools.CanSet(true, Main.tile[p.X, y], materialType,
                                                        select, expression, magicWand, p.X, y, plr))
                                                    {
                                                        SetTile(p.X, y, materialType);
                                                        edits++;
                                                    }
                                                }
                                            }
                                        }
                                        break;
                                    }

                                #endregion
                                #region Left

                                case 2:
                                    {
                                        if (wall)
                                        {
                                            foreach (WEPoint p in points)
                                            {
                                                for (int x = p.X; x <= x2; x++)
                                                {
                                                    var tile = Main.tile[x, p.Y];
                                                    if (Tools.CanSet(false, Main.tile[x, p.Y], materialType,
                                                        select, expression, magicWand, x, p.Y, plr))
                                                    {
                                                        Main.tile[x, p.Y].wall = (ushort)materialType;
                                                        edits++;
                                                    }
                                                }
                                            }
                                        }
                                        else
                                        {
                                            foreach (WEPoint p in points)
                                            {
                                                for (int x = p.X; x <= x2; x++)
                                                {
                                                    var tile = Main.tile[x, p.Y];
                                                    if (Tools.CanSet(true, Main.tile[x, p.Y], materialType,
                                                        select, expression, magicWand, x, p.Y, plr))
                                                    {
                                                        SetTile(x, p.Y, materialType);
                                                        edits++;
                                                    }
                                                }
                                            }
                                        }
                                        break;
                                    }

                                #endregion
                                #region Right

                                case 3:
                                    {
                                        if (wall)
                                        {
                                            foreach (WEPoint p in points)
                                            {
                                                for (int x = p.X; x >= this.x; x--)
                                                {
                                                    var tile = Main.tile[x, p.Y];
                                                    if (Tools.CanSet(false, Main.tile[x, p.Y], materialType,
                                                        select, expression, magicWand, x, p.Y, plr))
                                                    {
                                                        Main.tile[x, p.Y].wall = (ushort)materialType;
                                                        edits++;
                                                    }
                                                }
                                            }
                                        }
                                        else
                                        {
                                            foreach (WEPoint p in points)
                                            {
                                                for (int x = p.X; x >= this.x; x++)
                                                {
                                                    var tile = Main.tile[x, p.Y];
                                                    if (Tools.CanSet(true, Main.tile[x, p.Y], materialType,
                                                        select, expression, magicWand, x, p.Y, plr))
                                                    {
                                                        SetTile(x, p.Y, materialType);
                                                        edits++;
                                                    }
                                                }
                                            }
                                        }
                                        break;
                                    }

                                #endregion
                                default: return;
                            }
                        }
                        else
                        {
                            #region Wall

                            if (wall)
                            {
                                foreach (WEPoint p in points)
                                {
                                    var tile = Main.tile[p.X, p.Y];
                                    if (Tools.CanSet(false, Main.tile[p.X, p.Y], materialType,
                                        select, expression, magicWand, p.X, p.Y, plr))
                                    {
                                        Main.tile[p.X, p.Y].wall = (ushort)materialType;
                                        edits++;
                                    }
                                }
                                for (int x = line1[0].X; x <= line1[1].X; x++)
                                {
                                    for (int y = line1[0].Y; y <= line1[1].Y; y++)
                                    {
                                        var tile = Main.tile[x, y];
                                        if (Tools.CanSet(true, Main.tile[x, y], materialType,
                                            select, expression, magicWand, x, y, plr))
                                        {
                                            Main.tile[x, y].wall = (ushort)materialType;
                                            edits++;
                                        }
                                    }
                                }
                                if (line2 != null)
                                {
                                    for (int x = line2[0].X; x <= line2[1].X; x++)
                                    {
                                        for (int y = line2[0].Y; y <= line2[1].Y; y++)
                                        {
                                            var tile = Main.tile[x, y];
                                            if (Tools.CanSet(true, Main.tile[x, y], materialType,
                                                select, expression, magicWand, x, y, plr))
                                            {
                                                Main.tile[x, y].wall = (ushort)materialType;
                                                edits++;
                                            }
                                        }
                                    }
                                }
                            }

                            #endregion
                            #region Tile

                            else
                            {
                                foreach (WEPoint p in points)
                                {
                                    var tile = Main.tile[p.X, p.Y];
                                    if (Tools.CanSet(true, Main.tile[p.X, p.Y], materialType,
                                        select, expression, magicWand, p.X, p.Y, plr))
                                    {
                                        SetTile(p.X, p.Y, materialType);
                                        edits++;
                                    }
                                }
                                for (int x = line1[0].X; x <= line1[1].X; x++)
                                {
                                    for (int y = line1[0].Y; y <= line1[1].Y; y++)
                                    {
                                        var tile = Main.tile[x, y];
                                        if (Tools.CanSet(true, Main.tile[x, y], materialType,
                                            select, expression, magicWand, x, y, plr))
                                        {
                                            SetTile(x, y, materialType);
                                            edits++;
                                        }
                                    }
                                }
                                if (line2 != null)
                                {
                                    for (int x = line2[0].X; x <= line2[1].X; x++)
                                    {
                                        for (int y = line2[0].Y; y <= line2[1].Y; y++)
                                        {
                                            var tile = Main.tile[x, y];
                                            if (Tools.CanSet(true, Main.tile[x, y], materialType,
                                                select, expression, magicWand, x, y, plr))
                                            {
                                                SetTile(x, y, materialType);
                                                edits++;
                                            }
                                        }
                                    }
                                }
                            }

                            #endregion
                        }

                        break;
                    }

                    #endregion
            }

            ResetSection();
            plr.SendSuccessMessage("Set {0}{1} shape. ({2})", filled ? "filled " : "", wall ? "wall" : "tile", edits);
        }
    }
}