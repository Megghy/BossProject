using BossFramework.DB;
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
        public int CellWidth { get; set; }

        /// <summary> 小块区域的高 </summary>
        public int CellHeight { get; set; }

        /// <summary> 整片属地的拥有者 </summary>
        public string Owner { get; set; }

        /// <summary>
        /// 小块区域的引用. 其中数组索引就是 <see cref="Cell.Id"/> ,
        /// 而顺序(数组索引)是按照 <see cref="GenerateCells"/> 中添加列表的顺序来
        /// </summary>
        public List<Cell> Cells { get; internal set; }

        public StructTile[,] TileData { get; set; }
        /// <summary>
        /// 区域的坐标信息
        /// </summary>
        [JsonMap]
        public CellPosition[] CellsPosition { get; set; }

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

            //draw horizental line
            for (var y = 0; y <= numY; y++)
            {
                for (var x = 0; x < Width; x++)
                {
                    for (var t = 0; t < style.LineWidth; t++)
                    {
                        TileHelper.SetTile(X + x, Y + y * cellY + t, style.TileId, style.TilePaint);
                        TileHelper.SetWall(X + x, Y + y * cellY + t, style.WallId, style.WallPaint);
                    }
                }
            }

            //draw vertical line
            for (var x = 0; x <= numX; x++)
            {
                for (var y = 0; y < Height; y++)
                {
                    for (var t = 0; t < style.LineWidth; t++)
                    {
                        TileHelper.SetTile(X + x * cellX + t, Y + y, style.TileId, style.TilePaint);
                        TileHelper.SetWall(X + x * cellX + t, Y + y, style.WallId, style.WallPaint);
                    }
                }
            }

            TileHelper.ResetSection(X, Y, Width, Height);

            var cellsPos = new List<CellPosition>();
            for (var x = 0; x < numX; x++)
            {
                for (var y = 0; y < numY; y++)
                {
                    cellsPos.Add(new()
                    {
                        X = X + x * cellX + style.LineWidth,
                        Y = Y + y * cellY + style.LineWidth,
                        Width = Width,
                        Height = Height
                    });
                }
            }
            CellsPosition = cellsPos.ToArray();
            PlotManager.UpdateCellsPos(this);
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
            return PlotManager.CurrentPlot.Cells.FirstOrDefault(c => c.Contains(tileX, tileY));
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
        /// <summary>
        /// 相对坐标
        /// </summary>
        public int X { get; set; }
        /// <summary>
        /// 相对坐标
        /// </summary>
        public int Y { get; set; }
        public int Width { get; set; }
        public int Height { get; set; }
    }
    internal sealed class Cell : UserConfigBase<Cell>
    {
        /// <summary>
        /// 所属区域
        /// </summary>
        public long PlotId { get; set; }
        /// <summary> Cell所属的 <see cref="Plot"/> 引用 </summary>
        public Plot Parent => PlotManager.Plots.FirstOrDefault(p => p.Id == PlotId);
        public bool IsVisiable => LastPositionIndex != -1;

        public int X => LastPositionIndex == -1 ? -1 : Parent.CellsPosition[LastPositionIndex]?.X ?? -1;
        public int Y => LastPositionIndex == -1 ? -1 : Parent.CellsPosition[LastPositionIndex]?.Y ?? -1;

        public int LastPositionIndex { get; set; } = 0;

        public Point Center => new(X + (Parent.CellWidth / 2), Y + (Parent.Height / 2));

        public int Width { get; set; }
        public int Height { get; set; }

        /// <summary> 属地的主人 </summary>
        public string Owner { get; set; }

        /// <summary>
        /// 玩家 <see cref="Owner"/> 领取属地的时间
        /// 用于判定过期 
        /// </summary>
        public DateTime GetTime { get; set; }

        public DateTime LastAccess { get; set; }

        /// <summary> 有权限动属地者 </summary>
        [JsonMap]
        public List<int> AllowedIDs { get; set; } = new();

        /// <summary>
        /// Removes a user's access to the region
        /// </summary>
        /// <param name="id">User ID to remove</param>
        /// <returns>true if the user was found and removed from the region's allowed users</returns>

        public void ClearTiles()
        {
            for (var i = X; i < X + Parent.CellWidth; i++)
            {
                for (var j = Y; j < Y + Parent.CellHeight; j++)
                {
                    Main.tile[i, j] = new Tile();
                }
            }
            TileHelper.ResetSection(X, Y, Parent.CellWidth, Parent.CellHeight);
        }
        public bool Contains(int x, int y)
        {
            return X <= x && x < X + Parent.CellWidth && Y <= y && y < Y + Parent.CellHeight;
        }
        public void GetInfo(TSPlayer receiver)
        {
            receiver.SendInfoMessage($"属地 {Id} - " +
                $"领主: {(string.IsNullOrWhiteSpace(Owner) ? "无" : Owner)}" +
                $" | 创建: {GetTime:g}" +
                $" | 修改: {LastAccess:g}" +
                $" | 最后一次生成坐标: {X} - {Y}");
        }
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
