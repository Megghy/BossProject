using MySqlConnector;
using System.Data;
using TShockAPI;
using TShockAPI.DB;

namespace Nanami
{
    internal class PvpDataManager
    {
        private readonly IDbConnection _database;

        public PvpDataManager(IDbConnection conn)
        {
            _database = conn;

            var table = new SqlTable("PvpRecord",
                new SqlColumn("Id", MySqlDbType.Int32) { Primary = true },
                new SqlColumn("Eliminations", MySqlDbType.Int32),
                new SqlColumn("Deaths", MySqlDbType.Int32),
                new SqlColumn("DamageDone", MySqlDbType.Int32),
                new SqlColumn("Endurance", MySqlDbType.Int32),
                new SqlColumn("KillStreak", MySqlDbType.Int32),
                new SqlColumn("BestKillStreak", MySqlDbType.Int32)
            );

            var creator = new SqlTableCreator(_database,
                                              _database.GetSqlType() == SqlType.Sqlite
                                                  ? (IQueryBuilder)new SqliteQueryCreator()
                                                  : new MysqlQueryCreator());
            creator.EnsureTableStructure(table);
        }

        public PlayerPvpData Load(TSPlayer player)
        {
            try
            {
                using (var reader = _database.QueryReader("SELECT * FROM PvpRecord WHERE Id=@0", player.Account.ID))
                {
                    if (reader?.Read() == true)
                        return PlayerPvpData.LoadFromDb(player.Index, reader);
                }
            }
            catch (Exception ex)
            {
                TShock.Log.Error(ex.ToString());
            }

            return new PlayerPvpData(player.Index);
        }

        public void Save(int userId, PlayerPvpData data)
        {
            bool exist;
            using (var reader = _database.QueryReader("SELECT * FROM PvpRecord WHERE Id=@0", userId))
            {
                exist = reader?.Read() == true;
            }

            try
            {
                _database.Query(exist
                    ? "UPDATE PvpRecord SET Eliminations = @0, Deaths = @1, DamageDone = @2, Endurance = @3, KillStreak = @4, BestKillStreak = @5 WHERE Id = @6;"
                    : "INSERT INTO PvpRecord (Eliminations, Deaths, DamageDone, Endurance, KillStreak, BestKillStreak, Id) VALUES (@0, @1, @2, @3, @4, @5, @6);",
                    data.Eliminations,
                    data.Deaths,
                    data.DamageDone,
                    data.Endurance,
                    data.KillStreak,
                    data.BestKillStreak,
                    userId);
            }
            catch (Exception ex)
            {
                TShock.Log.Error(ex.ToString());
            }
        }
    }
}
