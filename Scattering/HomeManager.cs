using MySqlConnector;
using System.Data;
using Terraria;
using TShockAPI;
using TShockAPI.DB;

namespace Scattering
{
    internal class HomeManager
    {
        private readonly IDbConnection _database;

        private readonly List<PlayerHome> _homes = new List<PlayerHome>();
        internal static List<PlayerSpawn> _spawns;

        public HomeManager(IDbConnection db)
        {
            _database = db;
            SqlTable table = new SqlTable("PlayerHome", new SqlColumn("Id", MySqlDbType.Int32)
            {
                AutoIncrement = true,
                Primary = true
            }, new SqlColumn("UserId", MySqlDbType.Int32)
            {
                Unique = true
            }, new SqlColumn("Name", MySqlDbType.VarChar, 15)
            {
                NotNull = true,
                Unique = true
            }, new SqlColumn("X", MySqlDbType.Int32), new SqlColumn("Y", MySqlDbType.Int32), new SqlColumn("WorldId", MySqlDbType.VarChar, 50)
            {
                Unique = true
            });
            IQueryBuilder provider;
            if (db.GetSqlType() != SqlType.Sqlite)
            {
                IQueryBuilder queryBuilder = new MysqlQueryCreator();
                provider = queryBuilder;
            }
            else
            {
                IQueryBuilder queryBuilder2 = new SqliteQueryCreator();
                provider = queryBuilder2;
            }
            new SqlTableCreator(db, provider).EnsureTableStructure(table);
        }

        public void Reload()
        {
            try
            {
                using QueryResult queryResult = _database.QueryReader("SELECT * FROM `PlayerHome` WHERE `PlayerHome`.WorldID = @0", Main.worldID.ToString());
                _homes.Clear();
                while (queryResult != null && queryResult.Read())
                {
                    _homes.Add(PlayerHome.FromReader(queryResult));
                }
                _spawns = BossFramework.DB.DBTools.GetAll<PlayerSpawn>().ToList();
            }
            catch (Exception ex)
            {
                TShock.Log.Error(ex.ToString());
            }
        }

        public void Add(int userId, string name, int x, int y)
        {
            PlayerHome playerHome = Find(userId, name);
            if (playerHome != null)
            {
                playerHome.X = x;
                playerHome.Y = y;
                Update(playerHome);
                return;
            }
            try
            {
                _database.Query("INSERT INTO PlayerHome (UserId, Name, X, Y, WorldId) VALUES (@0, @1, @2, @3, @4)", userId, name, x, y, Main.worldID.ToString());
                _homes.Add(new PlayerHome
                {
                    Name = name,
                    UserId = userId,
                    X = x,
                    Y = y
                });
            }
            catch (Exception ex)
            {
                TShock.Log.Error(ex.ToString());
            }
        }

        public PlayerHome Find(int userId, string name)
        {
            return _homes.FirstOrDefault((PlayerHome p) => p.UserId == userId && p.Name.Equals(name, StringComparison.Ordinal));
        }

        private void Update(PlayerHome ph)
        {
            try
            {
                _database.Query("UPDATE PlayerHome SET X=@0, Y=@1 WHERE UserId=@2 AND Name=@3", ph.X, ph.Y, ph.UserId, ph.Name);
            }
            catch (Exception ex)
            {
                TShock.Log.Error(ex.ToString());
            }
        }
    }
}
