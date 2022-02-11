using BossFramework.DB;
using MySqlConnector;
using System.Data;
using System.Diagnostics;
using Terraria;
using TShockAPI;
using TShockAPI.DB;

namespace PlotMarker
{
    internal sealed class PlotManager
    {
        public List<Plot> Plots = new List<Plot>();

        private readonly IDbConnection _database;

        private readonly object _addCellLock = new object();

        public PlotManager(IDbConnection connection)
        {
            _database = connection;

            var plotTable = new SqlTable("Plots",
                new SqlColumn("Id", MySqlDbType.Int32) { AutoIncrement = true, Primary = true },
                new SqlColumn("Name", MySqlDbType.VarChar, 10) { NotNull = true, Unique = true },
                new SqlColumn("X", MySqlDbType.Int32),
                new SqlColumn("Y", MySqlDbType.Int32),
                new SqlColumn("Width", MySqlDbType.Int32),
                new SqlColumn("Height", MySqlDbType.Int32),
                new SqlColumn("CellWidth", MySqlDbType.Int32),
                new SqlColumn("CellHeight", MySqlDbType.Int32),
                new SqlColumn("WorldId", MySqlDbType.VarChar, 50),
                new SqlColumn("Owner", MySqlDbType.VarChar, 50)
            );

            var cellTable = new SqlTable("Cells",
                new SqlColumn("PlotId", MySqlDbType.Int32) { Unique = true },
                new SqlColumn("CellId", MySqlDbType.Int32) { Unique = true },
                new SqlColumn("X", MySqlDbType.Int32),
                new SqlColumn("Y", MySqlDbType.Int32),
                new SqlColumn("Owner", MySqlDbType.VarChar, 50),
                new SqlColumn("UserIds", MySqlDbType.Text),
                new SqlColumn("GetTime", MySqlDbType.Text),
                new SqlColumn("LastAccess", MySqlDbType.Text)
            );

            var creator = new SqlTableCreator(_database,
                                              _database.GetSqlType() == SqlType.Sqlite
                                                  ? (IQueryBuilder)new SqliteQueryCreator()
                                                  : new MysqlQueryCreator());

            creator.EnsureTableStructure(plotTable);
            creator.EnsureTableStructure(cellTable);
        }

        public void Reload()
        {
            Plots.Clear();

            DBTools.SQL.Select<Plot>()
                        .Where(p => p.WorldId == Main.worldID)
                        .ToList()
                        .ForEach(plot =>
                        {
                            plot.Cells = LoadCells(plot);
                            Plots.Add(plot);
                        });
        }

        public Cell[] LoadCells(Plot parent)
        {
            var list = new List<Cell>();
            using (var reader = _database.QueryReader("SELECT * FROM `cells` WHERE `cells`.`PlotId` = @0 ORDER BY `cells`.`CellId` ASC",
                parent.Id))
            {
                while (reader.Read())
                {
                    DBTools.SQL.Select<Cell>()
                        .Where(c => c.PlotId == parent.Id)
                        .ToList()
                        .ForEach(cell => list.Add(cell));
                    var cell = new Cell
                    {
                        Parent = parent,
                        Id = reader.Get<int>("CellId"),
                        X = reader.Get<int>("X"),
                        Y = reader.Get<int>("Y"),
                        Owner = reader.Get<string>("Owner"),
                        GetTime = DateTime.TryParse(reader.Get<string>("GetTime"), out DateTime dt) ? dt : default(DateTime),
                        LastAccess = DateTime.TryParse(reader.Get<string>("LastAccess"), out dt) ? dt : default(DateTime),
                        AllowedIDs = new List<int>()
                    };
                }
            }
            return list.ToArray();
        }

        public bool AddPlot(int x, int y, int width, int height, string name, string owner, string worldid, Style style)
        {
            try
            {
                if (GetPlotByName(name) != null)
                {
                    return false;
                }

                _database.Query(
                    "INSERT INTO Plots (Name, X, Y, Width, Height, CellWidth, CellHeight, WorldId, Owner) VALUES (@0, @1, @2, @3, @4, @5, @6, @7, @8);",
                    name, x, y, width, height, style.CellWidth, style.CellHeight, worldid, owner);

                using (var res = _database.QueryReader("SELECT Id FROM Plots WHERE Name = @0 AND WorldId = @1",
                    name, worldid))
                {
                    if (res.Read())
                    {
                        Plots.Add(new Plot
                        {
                            Id = res.Get<int>("Id"),
                            Name = name,
                            X = x,
                            Y = y,
                            Width = width,
                            Height = height,
                            CellHeight = style.CellHeight,
                            CellWidth = style.CellWidth,
                            Owner = owner
                        });

                        return true;
                    }
                    return false;
                }
            }
            catch (MySqlException ex)
            {
                if (ex.Number == (int)MySqlErrorCode.DuplicateKeyEntry)
                {
                    return false;
                }
                TShock.Log.Error(ex.ToString());
            }
            catch (Exception ex)
            {
                TShock.Log.Error(ex.ToString());
            }
            return false;
        }

        public bool DelPlot(Plot plot)
        {
            if (plot == null || !Plots.Contains(plot))
            {
                return false;
            }
            try
            {
                Plots.Remove(plot);
                _database.Query("DELETE FROM Cells WHERE PlotId=@0", plot.Id);
                _database.Query("DELETE FROM Plots WHERE Id = @0", plot.Id);
                return true;
            }
            catch (Exception e)
            {
                TShock.Log.ConsoleError("[PlotMarker] 删除属地期间出现异常.");
                TShock.Log.Error(e.ToString());
            }
            return false;
        }

        public void AddCells(Plot plot)
        {
            Task.Run(() =>
            {
                lock (_addCellLock)
                {
                    _database.Query("DELETE FROM Cells WHERE PlotId=@0", plot.Id);
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

        public void AddCell(Cell cell)
        {
            try
            {
                if (_database.Query(
                    "INSERT INTO Cells (PlotId, CellId, X, Y, UserIds, Owner, GetTime, LastAccess) VALUES (@0, @1, @2, @3, @4, @5, @6, @7);",
                    cell.Parent.Id, cell.Id, cell.X, cell.Y, string.Empty, string.Empty, string.Empty, string.Empty) == 1)
                    return;
                throw new Exception("No affected rows.");
            }
            catch (Exception e)
            {
                TShock.Log.ConsoleError("[PM] Cell数值导入数据库失败. ({0}: {1})", cell.Parent.Name, cell.Id);
                TShock.Log.Error(e.ToString());
            }
        }

        public async void ApplyForCell(TSPlayer player)
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

        public void ApplyForCell(TSPlayer player, int tileX, int tileY)
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

        private void Apply(TSPlayer player, Cell cell)
        {
            cell.Owner = player.Name;
            cell.GetTime = DateTime.Now;

            _database.Query("UPDATE `cells` SET `Owner` = @0, `GetTime` = @1 WHERE `cells`.`CellId` = @2 AND `cells`.`PlotId` = @3;",
                player.Account.Name,
                DateTime.Now.ToString("s"),
                cell.Id,
                cell.Parent.Id);

            player.SendSuccessMessage("系统已经分配给你一块地.");
        }

        public bool AddCellUser(Cell cell, UserAccount user)
        {
            try
            {
                var mergedIDs = string.Empty;
                using (
                    var reader = _database.QueryReader("SELECT UserIds FROM Cells WHERE PlotId=@0 AND CellId=@1",
                                                      cell.Parent.Id, cell.Id))
                {
                    if (reader.Read())
                        mergedIDs = reader.Get<string>("UserIds");
                }

                var ids = mergedIDs.Split(',');
                var userId = user.ID.ToString();

                if (ids.Contains(userId))
                    return true;

                mergedIDs = string.IsNullOrEmpty(mergedIDs) ? userId : string.Concat(mergedIDs, ",", userId);

                var q = _database.Query("UPDATE Cells SET UserIds=@0 WHERE PlotId=@1 AND CellId=@2",
                    mergedIDs, cell.Parent.Id, cell.Id);
                cell.SetAllowedIDs(mergedIDs);
                return q != 0;
            }
            catch (Exception ex)
            {
                TShock.Log.Error(ex.ToString());
            }
            return false;
        }

        public bool RemoveCellUser(Cell cell, UserAccount user)
        {
            if (cell != null)
            {
                if (!cell.RemoveId(user.ID))
                {
                    return false;
                }

                var ids = string.Join(",", cell.AllowedIDs);
                return _database.Query("UPDATE Cells SET UserIds=@0 WHERE WHERE PlotId=@1 AND CellId=@2", ids,
                    cell.Parent.Id, cell.Id) > 0;
            }

            return false;
        }

        public void UpdateLastAccess(Cell cell)
        {
            Task.Run(() =>
            {
                try
                {
                    _database.Query("UPDATE Cells SET LastAccess=@0 WHERE PlotId=@1 AND CellId=@2",
                        cell.LastAccess.ToString("s"),
                        cell.Parent.Id,
                        cell.Id);
                }
                catch (Exception ex)
                {
                    TShock.Log.Error(ex.ToString());
                }
            });
        }

        public async Task<Cell> GetClearedCell()
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

        public Cell GetCellByPosition(int tileX, int tileY)
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

        public Plot GetPlotByName(string plotname)
        {
            return Plots.FirstOrDefault(p => p.Name.Equals(plotname, StringComparison.Ordinal));
        }

        public void FuckCell(Cell cell)
        {
            _database.Query("UPDATE `cells` SET `GetTime` = @0, `Owner` = @1, `UserIds` = @2, `LastAccess` = @3 WHERE `cells`.`PlotId` = @4 AND `cells`.`CellId` = @5;",
                string.Empty, string.Empty, string.Empty, string.Empty,
                cell.Parent.Id,
                cell.Id);
            cell.Owner = string.Empty;
            cell.GetTime = default(DateTime);
            cell.LastAccess = default(DateTime);
            cell.AllowedIDs.Clear();
            cell.ClearTiles();
        }

        public int GetTotalCells(string playerName)
        {
            const string query = @"SELECT COUNT(*) FROM `cells`, `plots`
WHERE `plots`.`WorldId` = @0
AND `cells`.`PlotId` = `plots`.`Id`
AND `cells`.`Owner` = @1";
            using (var reader = _database.QueryReader(query, Main.worldID.ToString(), playerName))
            {
                if (reader.Read())
                {
                    return reader.Get<int>("COUNT(*)");
                }
            }
            throw new Exception("数据库错误");
        }

        public Cell GetOnlyCellOfPlayer(string name)
        {
            return GetCellsOfPlayer(name).SingleOrDefault();
        }

        public Cell[] GetCellsOfPlayer(string name)
        {
            return (from plot in Plots
                    from cell in plot.Cells
                    where cell.Owner.Equals(name, StringComparison.Ordinal)
                    select cell).ToArray();
        }

        public void ChangeOwner(Cell cell, UserAccount user)
        {
            cell.Owner = user.Name;
            cell.GetTime = DateTime.Now;

            _database.Query("UPDATE `cells` SET `Owner` = @0, `GetTime` = @1 WHERE `cells`.`PlotId` = @2 AND `cells`.`CellId` = @3;",
                user.Name,
                cell.GetTime.ToString("s"),
                cell.Parent.Id,
                cell.Id);
        }
    }
}
