using BossFramework.DB;
using System.Data;
using Terraria;
using TShockAPI;
using TShockAPI.DB;

namespace PlotMarker
{
    internal static class PlotManager
    {
        public static List<Plot> Plots = new();
        public static Plot CurrentPlot => Plots.FirstOrDefault(p => p.WorldId == Main.worldID);

        public static void Reload()
        {
            Plots.Clear();

            DBTools.GetAll<Plot>(p => p.WorldId == Main.worldID)
                        .ForEach(plot =>
                        {
                            plot.Cells = LoadCells(plot);
                            Plots.Add(plot);
                        });
        }

        public static List<Cell> LoadCells(Plot parent)
            => DBTools.GetAll<Cell>(c => c.PlotId == parent.Id).ToList();

        public static bool AddPlot(int x, int y, int width, int height, string name, string owner, long worldid, Style style)
        {
            if (GetPlotByName(name) != null)
            {
                return false;
            }

            var plot = new Plot()
            {
                Name = name,
                Owner = owner,
                WorldId = worldid,
                X = x,
                Y = y,
                Width = width,
                Height = height,
                PlotStyle = style
            };
            if (!DBTools.Exist<Plot>(p => p.Name == name && p.WorldId == worldid))
            {
                DBTools.Insert(plot);
                Plots.Add(plot);
                return true;
            }
            else
                return false;
        }

        public static bool DelPlot(Plot plot)
        {
            Plots.Remove(plot);
            DBTools.Delete<Cell>(c => c.PlotId == plot.Id);
            DBTools.Delete<Plot>(plot.Id);
            return true;
        }

        public static void UpdateCellsPos(Plot plot)
            => Task.Run(() => plot.UpdateSingle(p => p.CellsPosition));

        #region 子属地可见性操作
        /// <summary>
        /// 寻找可以放得下属地的区域
        /// </summary>
        /// <param name="count"></param>
        /// <returns>左上角的子区域位置</returns>
        public static CellPosition[] FindUseableArea(this Cell cell, bool isFirstRun = true)
        {
            List<CellPosition> cellPosList = new();
            for (var x = 0; x < CurrentPlot.CellsPosition2D.GetLength(0); x++)
            {
                for (var y = 0; y < CurrentPlot.CellsPosition2D.GetLength(1); y++)
                {
                    for (int tempX = 0; tempX < cell.Level; tempX++)
                    {
                        for (int tempY = 0; tempY < cell.Level; tempY++)
                        {
                            var cellPos = CurrentPlot.CellsPosition2D[x + tempY, y + tempY];
                            if (cellPos.IsUsed())
                                goto loop;
                            else
                                cellPosList.Add(cellPos);
                        }
                    }
                    return cellPosList.ToArray();
                loop:
                    cellPosList.Clear();
                }
            }
            //跑完一圈依然没有能用的
            CurrentPlot.Cells.Where(c =>
                {
                    var plr = TShock.Players.FirstOrDefault(p => p.Name == c.Owner);
                    if (plr is null || c.Contains(plr.TileX, plr.TileY))
                        return true;
                    return false;
                })
                .ForEach(c =>
                {
                    c.ClearTiles(false);
                });
            TileHelper.ResetSection(CurrentPlot.X, CurrentPlot.Y, CurrentPlot.Width, CurrentPlot.Height);
            if (isFirstRun)
                return cell.FindUseableArea(false);
            else
                return null;
        }
        
        #endregion

        public static void CreateNewCell(TSPlayer player)
        {
            try
            {
                var cell = new Cell()
                {
                    CreateTime = DateTime.Now,
                    PlotId = CurrentPlot.Id,
                    AllowedIDs = new(),
                    LastPositionIndex = -1
                };
                Apply(player, cell);
                player.Teleport(cell.Center.X * 16, cell.Center.Y * 16);
            }
            catch (Exception ex)
            {
                TShock.Log.Error(ex.ToString());
                player.SendErrorMessage("系统错误, 获取属地失败. 请联系管理.");
            }
        }

        #region 子属地操作

        private static void Apply(TSPlayer player, Cell cell)
        {
            cell.Owner = player.Name;
            cell.GetTime = DateTime.Now;

            DBTools.SQL.Update<Cell>(cell)
                .Set(c => c.Owner)
                .Set(c => c.GetTime)
                .ExecuteAffrows();

            player.SendSuccessMessage("系统已经分配给你一块地.");
        }

        public static bool AddCellUser(Cell cell, UserAccount user)
        {
            var realCell = DBTools.Get<Cell>(c => c.Id == cell.Id);
            realCell.AllowedIDs.Add(user.ID);
            cell.AllowedIDs = realCell.AllowedIDs;

            return cell.UpdateSingle(c => c.AllowedIDs) == 1;
        }

        public static bool RemoveCellUser(Cell cell, UserAccount user)
        {
            if (cell != null)
            {
                if (!cell.AllowedIDs.Remove(user.ID))
                {
                    return false;
                }

                return cell.UpdateSingle(c => c.AllowedIDs) == 1;
            }

            return false;
        }

        public static void UpdateLastAccess(Cell cell)
            => Task.Run(() => cell.UpdateSingle(c => c.LastAccess, DateTime.Now));

        public static Cell GetCellByPosition(int tileX, int tileY)
        {
            var plot = Plots.FirstOrDefault(p => p.Contains(tileX, tileY));
            if (plot == null)
            {
                return null;
            }
            if (plot.IsWall(tileX, tileY))
            {
                return null;
            }
            return plot.FindCell(tileX, tileY);
        }
        public static void FuckCell(Cell cell)
        {
            if (cell.IsVisiable)
                cell.Invisiable();
            cell.Owner = string.Empty;
            cell.GetTime = default;
            cell.LastAccess = default;
            cell.AllowedIDs.Clear();
            cell.UsingCellPosition.Clear();
            cell.LastPositionIndex = -1;
            Array.Clear(cell.TileData);
            cell.SerializedTileData = null;
            cell.ClearTiles();

            DBTools.SQL.Update<Cell>(cell)
                .Set(c => c.Owner)
                .Set(c => c.GetTime)
                .Set(c => c.LastAccess)
                .Set(c => c.AllowedIDs)
                .Set(c => c.SerializedTileData)
                .ExecuteAffrows();
        }
        #endregion

        public static Plot GetPlotByName(string plotname)
        {
            return Plots.FirstOrDefault(p => p.Name.Equals(plotname, StringComparison.Ordinal));
        }

        public static int GetTotalCells(string playerName)
            => (int)DBTools.SQL.Select<Cell>()
                .Where(c => c.Owner == playerName)
                .Count();

        public static Cell GetOnlyCellOfPlayer(string name)
        {
            return GetCellsOfPlayer(name).SingleOrDefault();
        }
        public static Cell[] GetCellsOfPlayer(string name)
        {
            return (from plot in Plots
                    from cell in plot.Cells
                    where cell.Owner.Equals(name, StringComparison.Ordinal)
                    select cell).ToArray();
        }

        public static void ChangeOwner(Cell cell, UserAccount user)
        {
            cell.Owner = user.Name;
            cell.GetTime = DateTime.Now;

            DBTools.SQL.Update<Cell>(cell)
                .Set(c => c.Owner)
                .Set(c => c.GetTime)
                .ExecuteAffrows();
        }
    }
}
