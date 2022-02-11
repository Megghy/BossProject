using BossFramework.DB;
using System.Data;
using System.Diagnostics;
using Terraria;
using TShockAPI;
using TShockAPI.DB;

namespace PlotMarker
{
    internal static class PlotManager
    {
        public static List<Plot> Plots = new();

        private static readonly object _addCellLock = new();

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

        public static Cell[] LoadCells(Plot parent)
            => DBTools.GetAll<Cell>(c => c.PlotId == parent.Id);

        public  static bool AddPlot(int x, int y, int width, int height, string name, string owner, long worldid, Style style)
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
                Width = style.CellWidth,
                Height = style.CellHeight
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

        public static void AddCells(Plot plot)
        {
            Task.Run(() =>
            {
                lock (_addCellLock)
                {
                    DBTools.Delete<Cell>(c => c.Id == plot.Id);
                    var stopwatch = new Stopwatch();
                    stopwatch.Start();
                    var count = 0;
                    foreach (var cell in plot.Cells)
                    {
                        AddCell(cell);
                        count++;
                    }
                    stopwatch.Stop();
                    TShock.Log.Info("记录完毕. 共有{0}个. ({1}ms)", count, stopwatch.ElapsedMilliseconds);
                }
            });
        }

        public static void AddCell(Cell cell)
        {
            try
            {
                if (DBTools.Insert(new Cell()
                {
                    Id = cell.Id,
                    PlotId = cell.PlotId,
                    X = cell.X,
                    Y = cell.Y,
                    AllowedIDs = cell.AllowedIDs,
                    Owner = cell.Owner
                }) == 1)
                    return;
                throw new Exception("No affected rows.");
            }
            catch (Exception e)
            {
                TShock.Log.ConsoleError("[PM] Cell数值导入数据库失败. ({0}: {1})", cell.Parent.Name, cell.Id);
                TShock.Log.Error(e.ToString());
            }
        }

        public static async void ApplyForCell(TSPlayer player)
        {
            try
            {
                var cell = Plots
                                .SelectMany(plot => plot.Cells)
                                .LastOrDefault(c => string.IsNullOrWhiteSpace(c.Owner));
                if (cell == null)
                {
                    cell = await GetClearedCell();
                    if (cell == null)
                    {
                        player.SendWarningMessage("现在没有可用属地了.. 请联系管理.");
                        return;
                    }
                }
                Apply(player, cell);
                player.Teleport(cell.Center.X * 16, cell.Center.Y * 16);
            }
            catch (Exception ex)
            {
                TShock.Log.Error(ex.ToString());
                player.SendErrorMessage("系统错误, 获取属地失败. 请联系管理.");
            }
        }

        public static void ApplyForCell(TSPlayer player, int tileX, int tileY)
        {
            var cell = GetCellByPosition(tileX, tileY);
            if (cell == null)
            {
                player.SendErrorMessage("在选中点位置没有属地.");
                return;
            }
            if (!string.IsNullOrWhiteSpace(cell.Owner) && !player.HasPermission("plotmarker.admin.editall"))
            {
                player.SendErrorMessage("该属地已被占用.");
                return;
            }
            Apply(player, cell);
        }

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
                if (!cell.RemoveId(user.ID))
                {
                    return false;
                }

                cell.AllowedIDs.Remove(user.ID);
                return cell.UpdateSingle(c => c.AllowedIDs) == 1;
            }

            return false;
        }

        public static void UpdateLastAccess(Cell cell)
            => Task.Run(() => cell.UpdateSingle(c => c.LastAccess, DateTime.Now));

        public static async Task<Cell> GetClearedCell()
        {
            return await Task.Run(() =>
            {
                var cells =
                    from p in Plots
                    from c in p.Cells
                    where (DateTime.Now - c.LastAccess).Days > 4
                    orderby c.LastAccess
                    select c;

                var cell = cells.FirstOrDefault();
                if (cell == null)
                    return null;

                FuckCell(cell);

                return cell;
            });
        }

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
            var index = plot.FindCell(tileX, tileY);
            if (index > -1 && index < plot.Cells.Length)
            {
                return plot.Cells[index];
            }
            return null;
        }

        public static Plot GetPlotByName(string plotname)
        {
            return Plots.FirstOrDefault(p => p.Name.Equals(plotname, StringComparison.Ordinal));
        }

        public static void FuckCell(Cell cell)
        {
            cell.Owner = string.Empty;
            cell.GetTime = default;
            cell.LastAccess = default;
            cell.AllowedIDs.Clear();
            cell.ClearTiles();

            DBTools.SQL.Update<Cell>(cell)
                .Set(c => c.Owner)
                .Set(c => c.GetTime)
                .Set(c => c.LastAccess)
                .Set(c => c.AllowedIDs)
                .ExecuteAffrows();
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
