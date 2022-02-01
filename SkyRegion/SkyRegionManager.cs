using System.Data;
using MySqlConnector;
using Terraria;
using TShockAPI;
using TShockAPI.DB;

namespace SkyRegion
{
    internal class SkyRegionManager
    {
        public List<Region> SrRegions = new List<Region>();

        private readonly IDbConnection _database;

        public SkyRegionManager(IDbConnection db)
        {
            _database = db;

            var table = new SqlTable("SkyRegions",
                new SqlColumn("Id", MySqlDbType.Int32) { Primary = true, AutoIncrement = true, NotNull = true },
                new SqlColumn("RegionId", MySqlDbType.Int32) { Unique = true }
            );

            var creator = new SqlTableCreator(db,
                                              db.GetSqlType() == SqlType.Sqlite
                                                  ? (IQueryBuilder)new SqliteQueryCreator()
                                                  : new MysqlQueryCreator());
            creator.EnsureTableStructure(table);
        }

        public void LoadRegions()
        {
            try
            {
                using (
                    var reader =
                        _database.QueryReader(
                            "SELECT `SkyRegions`.* FROM `SkyRegions`, `regions` WHERE `SkyRegions`.RegionId = `regions`.Id AND `regions`.WorldID = @0",
                            Main.worldID.ToString())
                    )
                {
                    SrRegions.Clear();

                    while (reader.Read())
                    {
                        SrRegions.Add(TShock.Regions.GetRegionByID(reader.Get<int>("RegionId")));
                    }
                }
            }
            catch (Exception ex)
            {
                TShock.Log.Error(ex.ToString());
            }
        }

        public void Add(Region region)
        {
            if (SrRegions.Contains(region))
                return;

            SrRegions.Add(region);

            try
            {
                _database.Query("INSERT INTO SkyRegions (RegionId) VALUES (@0);", region.ID);
            }
            catch (Exception ex)
            {
                TShock.Log.Error(ex.ToString());
            }
        }

        public void Remove(Region region)
        {
            if (!SrRegions.Contains(region))
                return;

            SrRegions.Remove(region);

            try
            {
                _database.Query("DELETE FROM SkyRegions WHERE RegionId = @0", region.ID);
            }
            catch (Exception ex)
            {
                TShock.Log.Error(ex.ToString());
            }
        }
    }
}
