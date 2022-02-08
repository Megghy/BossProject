using MySqlConnector;
using System.Data;
using System.Text;
using Terraria;
using TShockAPI;
using TShockAPI.DB;

namespace RegionTrigger
{
    internal sealed class RtRegionManager
    {
        public readonly List<RtRegion> Regions = new();

        private readonly IDbConnection _database;

        internal RtRegionManager(IDbConnection db)
        {
            _database = db;

            var table = new SqlTable("RtRegions",
                                     new SqlColumn("Id", MySqlDbType.Int32) { Primary = true, AutoIncrement = true },
                                     new SqlColumn("RegionId", MySqlDbType.Int32) { Unique = true, NotNull = true },
                                     new SqlColumn("Events", MySqlDbType.Text),
                                     new SqlColumn("EnterMsg", MySqlDbType.Text),
                                     new SqlColumn("LeaveMsg", MySqlDbType.Text),
                                     new SqlColumn("Message", MySqlDbType.Text),
                                     new SqlColumn("MessageInterval", MySqlDbType.Int32),
                                     new SqlColumn("TempGroup", MySqlDbType.String, 32),
                                     new SqlColumn("Itembans", MySqlDbType.Text),
                                     new SqlColumn("Projbans", MySqlDbType.Text),
                                     new SqlColumn("Tilebans", MySqlDbType.Text),
                                     new SqlColumn("Permissions", MySqlDbType.Text)
            );

            var creator = new SqlTableCreator(db,
                                              db.GetSqlType() == SqlType.Sqlite
                                                  ? (IQueryBuilder)new SqliteQueryCreator()
                                                  : new MysqlQueryCreator());
            creator.EnsureTableStructure(table);
        }

        public void Reload()
        {
            try
            {
                using (
                    var reader =
                        _database.QueryReader(
                            "SELECT `rtregions`.* FROM `rtregions`, `regions` WHERE `rtregions`.RegionId = `regions`.Id AND `regions`.WorldID = @0",
                            Main.worldID.ToString())
                    )
                {
                    Regions.Clear();

                    while (reader.Read())
                    {
                        Regions.Add(RtRegion.FromReader(reader));
                    }
                }
            }
            catch (Exception e)
            {
                TShock.Log.ConsoleError("[RegionTrigger] Load regions failed. Check log for more information.");
                TShock.Log.Error(e.ToString());
            }
        }

        public void AddRtRegion(int regionId)
        {
            if (Regions.Any(r => r.Region.ID == regionId))
                return;

            var rt = new RtRegion(-1, regionId, Event.None);

            const string query = "INSERT INTO RtRegions (RegionId, Events) VALUES (@0, @1);";
            try
            {
                _database.Query(query, regionId, rt.Events);
                using (var result = _database.QueryReader("SELECT Id FROM RtRegions WHERE RegionId = @0", regionId))
                {
                    if (result.Read())
                    {
                        rt.Id = result.Get<int>("Id");
                        Regions.Add(rt);
                    }
                    else
                        throw new Exception("Database error: No affected rows.");
                }
            }
            catch (Exception e)
            {
                TShock.Log.Error(e.ToString());
            }
        }

        public void DeleteRtRegion(int regionId)
        {
            var rt = GetRtRegionByRegionId(regionId);
            if (rt == null)
                return;

            try
            {
                _database.Query("DELETE FROM RtRegions WHERE RegionId = @0", rt.Region.ID);
                Regions.Remove(rt);
            }
            catch (Exception exception)
            {
                TShock.Log.Error(exception.ToString());
            }
        }

        public void AddEvents(RtRegion rt, Event ev)
        {
            rt.AddEvent(ev);

            _database.Query("UPDATE RtRegions SET Events = @0 WHERE Id = @1", rt.Events, rt.Id);
        }

        public void RemoveEvents(RtRegion rt, Event ev)
        {
            rt.RemoveEvent(ev);

            _database.Query("UPDATE RtRegions SET Events = @0 WHERE Id = @1", rt.Events, rt.Id);
        }

        public void SetTempGroup(RtRegion rt, string tempGroup)
        {
            var isNull = string.IsNullOrWhiteSpace(tempGroup);

            Group group = null;
            if (!isNull)
            {
                group = TShock.Groups.GetGroupByName(tempGroup);
                if (group == null)
                    throw new GroupNotExistException(tempGroup);
            }

            var query = isNull
                ? "UPDATE RtRegions SET TempGroup = NULL WHERE Id = @0"
                : "UPDATE RtRegions SET TempGroup = @0 WHERE Id = @1";
            var args = isNull
                ? new object[] { rt.Id }
                : new object[] { group.Name, rt.Id };

            _database.Query(query, args);

            rt.TempGroup = group;
        }

        public void SetMsgInterval(RtRegion rt, int interval)
        {
            if (interval < 0)
                throw new ArgumentException(@"Interval can't be lesser than zero!", nameof(interval));

            if (rt.MsgInterval == interval)
                return;

            _database.Query("UPDATE RtRegions SET MessageInterval = @0 WHERE Id = @1", interval, rt.Id);

            rt.MsgInterval = interval;
        }

        public void SetMessage(RtRegion rt, string message)
        {
            var isNull = string.IsNullOrWhiteSpace(message);
            if (rt.Message == message)
                return;

            var query = isNull
                ? "UPDATE RtRegions SET Message = NULL WHERE Id = @0"
                : "UPDATE RtRegions SET Message = @0 WHERE Id = @1";
            var args = isNull
                ? new object[] { rt.Id }
                : new object[] { message, rt.Id };

            _database.Query(query, args);

            rt.Message = message;
        }

        public void SetEnterMessage(RtRegion rt, string message)
        {
            var isNull = string.IsNullOrWhiteSpace(message);
            if (string.Equals(rt.EnterMsg, message))
                return;

            var query = isNull
                ? "UPDATE RtRegions SET EnterMsg = NULL WHERE Id = @0"
                : "UPDATE RtRegions SET EnterMsg = @0 WHERE Id = @1";
            var args = isNull
                ? new object[] { rt.Id }
                : new object[] { message, rt.Id };

            _database.Query(query, args);

            rt.EnterMsg = message;
        }

        public void SetLeaveMessage(RtRegion rt, string message)
        {
            var isNull = string.IsNullOrWhiteSpace(message);
            if (string.Equals(rt.LeaveMsg, message))
                return;

            var query = isNull
                ? "UPDATE RtRegions SET LeaveMsg = NULL WHERE Id = @0"
                : "UPDATE RtRegions SET LeaveMsg = @0 WHERE Id = @1";
            var args = isNull
                ? new object[] { rt.Id }
                : new object[] { message, rt.Id };

            _database.Query(query, args);

            rt.LeaveMsg = message;
        }

        public void AddItemban(RtRegion rt, string itemName)
        {
            if (rt.ItemIsBanned(itemName))
                return;

            var modified = new StringBuilder(rt.Itembans);
            if (modified.Length != 0)
                modified.Append(',');
            modified.Append(itemName);

            _database.Query("UPDATE RtRegions SET Itembans = @0 WHERE Id = @1", modified, rt.Id);

            rt.Itembans = modified.ToString();
        }

        public void RemoveItemban(RtRegion rt, string itemName)
        {
            if (!rt.ItemIsBanned(itemName))
                return;

            var origin = rt.Itembans;

            if (rt.RemoveBannedItem(itemName) &&
                _database.Query("UPDATE RtRegions SET Itembans = @0 WHERE Id = @1", rt.Itembans, rt.Id) != 0)
                return;

            rt.Itembans = origin;
            throw new Exception("Database error: No affected rows.");
        }

        public void AddProjban(RtRegion rt, short projId)
        {
            if (rt.ProjectileIsBanned(projId))
                return;

            var modified = new StringBuilder(rt.Projbans);
            if (modified.Length != 0)
                modified.Append(',');
            modified.Append(projId);

            _database.Query("UPDATE RtRegions SET Projbans = @0 WHERE Id = @1", modified, rt.Id);

            rt.Projbans = modified.ToString();
        }

        public void RemoveProjban(RtRegion rt, short projId)
        {
            if (!rt.ProjectileIsBanned(projId))
                return;

            var origin = rt.Projbans;

            if (rt.RemoveBannedProjectile(projId) &&
                _database.Query("UPDATE RtRegions SET Projbans = @0 WHERE Id = @1", rt.Projbans, rt.Id) != 0)
                return;

            rt.Projbans = origin;
            throw new Exception("Database error: No affected rows.");
        }

        public void AddTileban(RtRegion rt, short tileId)
        {
            if (rt.TileIsBanned(tileId))
                return;

            var modified = new StringBuilder(rt.Tilebans);
            if (modified.Length != 0)
                modified.Append(',');
            modified.Append(tileId);

            _database.Query("UPDATE RtRegions SET Tilebans = @0 WHERE Id = @1", modified, rt.Id);

            rt.Tilebans = modified.ToString();
        }

        public void RemoveTileban(RtRegion rt, short tileId)
        {
            if (!rt.TileIsBanned(tileId))
                return;

            var origin = rt.Tilebans;

            if (rt.RemoveBannedTile(tileId) &&
                _database.Query("UPDATE RtRegions SET Tilebans = @0 WHERE Id = @1", rt.Tilebans, rt.Id) != 0)
                return;

            rt.Tilebans = origin;
            throw new Exception("Database error: No affected rows.");
        }

        public void AddPermissions(RtRegion rt, List<string> permissions)
        {
            var origin = rt.Permissions;
            permissions.ForEach(rt.AddPermission);

            if (_database.Query("UPDATE RtRegions SET Permissions = @0 WHERE Id = @1", rt.Permissions, rt.Id) != 0)
                return;

            rt.Permissions = origin;
            throw new Exception("Database error: No affected rows.");
        }

        public void DeletePermissions(RtRegion rt, List<string> permissions)
        {
            var origin = rt.Permissions;
            permissions.ForEach(rt.RemovePermission);

            if (_database.Query("UPDATE RtRegions SET Permissions = @0 WHERE Id = @1", rt.Permissions, rt.Id) != 0)
                return;

            rt.Permissions = origin;
            throw new Exception("Database error: No affected rows.");
        }

        public RtRegion GetRtRegionByRegionId(int regionId)
            => Regions.SingleOrDefault(rt => regionId == rt.Region.ID);

        public RtRegion GetTopRegion(IEnumerable<RtRegion> regions)
        {
            RtRegion ret = null;
            foreach (var r in regions)
            {
                if (ret == null)
                    ret = r;
                else
                {
                    if (r.Region.Z > ret.Region.Z)
                        ret = r;
                }
            }
            return ret;
        }

        public RtRegion GetCurrentRegion(TSPlayer player)
        {
            return GetTopRegion(Regions.Where(r => r.Region.InArea(player.TileX, player.TileY)));
        }

        public class RegionDefinedException : Exception
        {
            public readonly string RegionName;

            public RegionDefinedException(string name) : base($"Region '{name}' was already defined!")
            {
                RegionName = name;
            }
        }
    }
}
