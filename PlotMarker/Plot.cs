using BossFramework.DB;
using Bssom.Serializer;
using FakeProvider;
using FreeSql.DataAnnotations;
using Microsoft.Xna.Framework;
using Newtonsoft.Json;
using Terraria;
using TShockAPI;

namespace PlotMarker
{
    internal sealed class Plot : UserConfigBase<Plot>
    {
        /// <summary>
        /// 所属世界
        /// </summary>
        public long WorldId { get; set; }
        /// <summary> 属地的名字 </summary>
        public string Name { get; set; }

        /// <summary> 属地的起始X坐标 </summary>
        public int X { get; set; }

        /// <summary> 属地的起始Y坐标 </summary>
        public int Y { get; set; }

        /// <summary> 属地的宽 </summary>
        public int Width { get; set; }

        /// <summary> 属地的高 </summary>
        public int Height { get; set; }

        /// <summary> 小块区域的宽 </summary>
        public int CellWidth => PlotStyle.CellWidth;

        /// <summary> 小块区域的高 </summary>
        public int CellHeight => PlotStyle.CellHeight;

        /// <summary>
        /// 属地之间间隔宽度
        /// </summary>
        public int LineWidth => PlotStyle.LineWidth;

        /// <summary> 整片属地的拥有者 </summary>
        public string Owner { get; set; }

        /// <summary>
        /// 小块区域的引用. 其中数组索引就是 <see cref="Cell.Id"/> ,
        /// 而顺序(数组索引)是按照 <see cref="GenerateCells"/> 中添加列表的顺序来
        /// </summary>
        public List<Cell> Cells { get; internal set; } = new List<Cell>();

        /// <summary>
        /// 区域的坐标信息
        /// </summary>
        [JsonMap]
        public List<CellPosition> CellsPosition { get; set; }
        private CellPosition[,] _cellsPosition2D;
        public CellPosition[,] CellsPosition2D
        {
            get
            {
                if (CellsPosition is null)
                    return null;
                if (_cellsPosition2D is null)
                {
                    _cellsPosition2D = new CellPosition[CellsPosition.Max(c => c.IndexX) + 1, CellsPosition.Max(c => c.IndexY) + 1];
                    CellsPosition.ForEach(c => _cellsPosition2D[c.IndexX, c.IndexY] = c);
                }
                return _cellsPosition2D;
            }
        }

        [JsonMap]
        public Style PlotStyle { get; set; }

        /// <summary>
        /// 生成格子并记录格子数值到数据库.
        /// </summary>
        /// <param name="empty"> 是否清空区域/适合修复格子 </param>
        public void GenerateCells(bool empty = true)
        {
            if (empty)
            {
                TileHelper.RemoveTiles(X, Y, Width, Height);
            }

            var style = PlotMarker.Config.PlotStyle;
            var cellX = CellWidth + style.LineWidth;
            var cellY = CellHeight + style.LineWidth;
            var numX = (Width - style.LineWidth) / cellX;
            var numY = (Height - style.LineWidth) / cellY;
            Width = numX * cellX + style.LineWidth;
            Height = numY * cellY + style.LineWidth;
            ReDrawLines(false);

            TileHelper.ResetSection(X, Y, Width, Height);

            var cellsPos = new List<CellPosition>();
            var index = 0;
            for (var x = 0; x < numX; x++)
            {
                for (var y = 0; y < numY; y++)
                {
                    cellsPos.Add(new()
                    {
                        TileX = X + x * cellX + style.LineWidth,
                        TileY = Y + y * cellY + style.LineWidth,
                        Width = Width,
                        Height = Height,
                        IndexX = x,
                        IndexY = y,
                        Index = index
                    });
                    index++;
                }
            }
            CellsPosition = cellsPos.ToList();
            PlotManager.UpdateCellsPos(this);
        }
        public void ReDrawLines(bool sendSection = true)
        {
            var cellX = CellWidth + PlotStyle.LineWidth;
            var cellY = CellHeight + PlotStyle.LineWidth;
            var numX = (Width - PlotStyle.LineWidth) / cellX;
            var numY = (Height - PlotStyle.LineWidth) / cellY;
            //draw horizental line
            for (var y = 0; y <= numY; y++)
            {
                for (var x = 0; x < Width; x++)
                {
                    for (var t = 0; t < PlotStyle.LineWidth; t++)
                    {
                        TileHelper.SetTile(X + x, Y + y * cellY + t, PlotStyle.TileId, PlotStyle.TilePaint);
                        TileHelper.SetWall(X + x, Y + y * cellY + t, PlotStyle.WallId, PlotStyle.WallPaint);
                    }
                }
            }

            //draw vertical line
            for (var x = 0; x <= numX; x++)
            {
                for (var y = 0; y < Height; y++)
                {
                    for (var t = 0; t < PlotStyle.LineWidth; t++)
                    {
                        TileHelper.SetTile(X + x * cellX + t, Y + y, PlotStyle.TileId, PlotStyle.TilePaint);
                        TileHelper.SetWall(X + x * cellX + t, Y + y, PlotStyle.WallId, PlotStyle.WallPaint);
                    }
                }
            }
            if (sendSection)
                TileHelper.ResetSection(X, Y, Width, Height);
        }

        public void Generate(bool clear = true)
        {
            Task.Run(() => GenerateCells(clear));
        }

        public bool Contains(int x, int y)
        {
            return X <= x && x < X + Width && Y <= y && y < Y + Height;
        }

        /// <summary>
        /// 根据物块坐标寻找Cell索引.
        /// </summary>
        /// <param name="tileX">物块X坐标(必须在属地内)</param>
        /// <param name="tileY">物块Y坐标(必须在属地内)</param>
        /// <returns><see cref="Cells"/>索引</returns>
        public Cell FindCell(int tileX, int tileY)
        {
            if (!Contains(tileX, tileY))
            {
                throw new ArgumentOutOfRangeException(nameof(tileX), "物块坐标必须在本属地内部!");
            }
            return PlotManager.CurrentPlot.Cells.FirstOrDefault(c => c.IsVisiable && c.Contains(tileX, tileY));
        }

        public bool IsWall(int tileX, int tileY)
        {
            var style = PlotMarker.Config.PlotStyle;
            var cellX = CellWidth + style.LineWidth;
            var cellY = CellHeight + style.LineWidth;

            return (tileX - X) % cellX < style.LineWidth || (tileY - Y) % cellY < style.LineWidth;
        }
    }
    internal record CellPosition
    {
        public int Index { get; set; }
        public int IndexX { get; set; }
        public int IndexY { get; set; }
        public int TileX { get; set; }
        public int TileY { get; set; }
        public int Width { get; set; }
        public int Height { get; set; }

        /// <summary>
        /// 是否被某个子区域占用
        /// </summary>
        /// <returns></returns>
        public bool IsUsed()
            => PlotManager.CurrentPlot.Cells.Exists(c => c.UsingCellPositionIndex.Contains(Index));
    }
    

    internal sealed class Style
    {
        [JsonProperty("间距")]
        public int LineWidth { get; set; }

        [JsonProperty("属地宽")]
        public int CellWidth { get; set; }

        [JsonProperty("属地高")]
        public int CellHeight { get; set; }

        [JsonProperty("物块")]
        public short TileId { get; set; }

        [JsonProperty("物块喷漆")]
        public byte TilePaint { get; set; }

        [JsonProperty("墙壁")]
        public short WallId { get; set; }

        [JsonProperty("墙壁喷漆")]
        public byte WallPaint { get; set; }
    }
}
