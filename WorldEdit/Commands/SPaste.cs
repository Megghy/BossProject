using OTAPI.Tile;
using Terraria;
using TShockAPI;
using WorldEdit.Expressions;

namespace WorldEdit.Commands
{
    public class SPaste : WECommand
    {
        private readonly int alignment;
        private readonly Expression expression;
        private readonly bool tiles;
        private readonly bool tilePaints;
        private readonly bool emptyTiles;
        private readonly bool walls;
        private readonly bool wallPaints;
        private readonly bool wires;
        private readonly bool liquids;

        public SPaste(int x, int y, TSPlayer plr, int alignment, Expression expression, bool tiles, bool tilePaints, bool emptyTiles, bool walls, bool wallPaints, bool wires, bool liquids)
            : base(x, y, int.MaxValue, int.MaxValue, plr)
        {
            this.alignment = alignment;
            this.expression = expression;
            this.tiles = tiles;
            this.tilePaints = tilePaints;
            this.emptyTiles = emptyTiles;
            this.walls = walls;
            this.wallPaints = wallPaints;
            this.wires = wires;
            this.liquids = liquids;
        }

        public override void Execute()
        {
            var clipboardPath = Tools.GetClipboardPath(plr.Account.ID);

            var data = Tools.LoadWorldData(clipboardPath);

            var width = data.Width - 1;
            var height = data.Height - 1;

            if ((alignment & 1) == 0)
                x2 = x + width;
            else
            {
                x2 = x;
                x -= width;
            }
            if ((alignment & 2) == 0)
                y2 = y + height;
            else
            {
                y2 = y;
                y -= height;
            }

            if (!CanUseCommand()) { return; }

            if (x < 0) { x = 0; }
            if (x2 < 0) { x2 = 0; }
            if (y < 0) { y = 0; }
            if (y2 < 0) { y2 = 0; }
            if (x >= Main.maxTilesX) { x = Main.maxTilesX - 1; }
            if (x2 >= Main.maxTilesX) { x2 = Main.maxTilesX - 1; }
            if (y >= Main.maxTilesY) { y = Main.maxTilesY - 1; }
            if (y2 >= Main.maxTilesY) { y2 = Main.maxTilesY - 1; }

            Tools.PrepareUndo(x, y, x2, y2, plr);

            for (var i = x; i <= x2; i++)
            {
                for (var j = y; j <= y2; j++)
                {
                    var index1 = i - x;
                    var index2 = j - y;

                    if (i < 0 || j < 0 || i >= Main.maxTilesX || j >= Main.maxTilesY ||
                        expression != null && !expression.Evaluate(data.Tiles[index1, index2]))
                    {
                        continue;
                    }

                    ITile tile = (ITile)Main.tile[i, j].Clone();

                    if (tiles) { tile = data.Tiles[index1, index2]; }
                    else
                    {
                        tile.wall = data.Tiles[index1, index2].wall;
                        tile.wallColor(data.Tiles[index1, index2].wallColor());
                        tile.liquid = data.Tiles[index1, index2].liquid;
                        tile.liquidType(data.Tiles[index1, index2].liquidType());
                        tile.wire(data.Tiles[index1, index2].wire());
                        tile.wire2(data.Tiles[index1, index2].wire2());
                        tile.wire3(data.Tiles[index1, index2].wire3());
                        tile.wire4(data.Tiles[index1, index2].wire4());
                        tile.actuator(data.Tiles[index1, index2].actuator());
                        tile.inActive(data.Tiles[index1, index2].inActive());
                    }

                    if (emptyTiles || tile.active() || (tile.wall != 0) || (tile.liquid != 0) || tile.wire() || tile.wire2() || tile.wire3() || tile.wire4())
                    {
                        if (!tilePaints)
                        { tile.color(Main.tile[i, j].color()); }
                        if (!walls)
                        {
                            tile.wall = Main.tile[i, j].wall;
                            tile.wallColor(Main.tile[i, j].wallColor());
                        }
                        if (!wallPaints)
                        { tile.wallColor(Main.tile[i, j].wallColor()); }
                        if (!liquids)
                        {
                            tile.liquid = Main.tile[i, j].liquid;
                            tile.liquidType(Main.tile[i, j].liquidType());
                        }
                        if (!wires)
                        {
                            tile.wire(Main.tile[i, j].wire());
                            tile.wire2(Main.tile[i, j].wire2());
                            tile.wire3(Main.tile[i, j].wire3());
                            tile.wire4(Main.tile[i, j].wire4());
                            tile.actuator(Main.tile[i, j].actuator());
                            tile.inActive(Main.tile[i, j].inActive());
                        }

                        Main.tile[i, j] = tile;
                    }
                }
            }

            Tools.LoadWorldSection(data, x, y, false);
            ResetSection();
            plr.SendSuccessMessage("Pasted clipboard to selection.");
        }
    }
}