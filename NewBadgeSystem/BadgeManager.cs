using System.Data;
using MySql.Data.MySqlClient;
using Newtonsoft.Json;
using TShockAPI;
using TShockAPI.DB;

namespace BadgeSystem
{
    public static class BadgeManager
    {
        //private readonly IDbConnection _database;

        public static void Init()
        {
            var _database = TShock.DB;
            SqlTable table = new SqlTable("NewBadge", new SqlColumn("Id", MySqlDbType.Int32)
            {
                Primary = true
            },
            new SqlColumn("TotalBrackets", MySqlDbType.Text),
            new SqlColumn("CurrentBrackets", MySqlDbType.Text),
            new SqlColumn("TotalPrefix", MySqlDbType.Text),
            new SqlColumn("CurrentPrefix", MySqlDbType.Text),
            new SqlColumn("TotalSuffix", MySqlDbType.Text),
            new SqlColumn("CurrentSuffix", MySqlDbType.Text));
            IDbConnection database = _database;
            IQueryBuilder provider;
            if (_database?.GetSqlType() != SqlType.Sqlite)
            {
                IQueryBuilder queryBuilder = new MysqlQueryCreator();
                provider = queryBuilder;
            }
            else
            {
                IQueryBuilder queryBuilder2 = new SqliteQueryCreator();
                provider = queryBuilder2;
            }
            SqlTableCreator sqlTableCreator = new SqlTableCreator(database, provider);
            sqlTableCreator.EnsureTableStructure(table);
        }

        public static Tuple<IEnumerable<string>, IEnumerable<string>>[] Load(int userId)
        {
            try
            {
                using QueryResult queryResult = TShock.DB.QueryReader("SELECT * FROM NewBadge WHERE Id=@0", userId);
                if (queryResult?.Read() ?? false)
                {
                    //TShock.Log.ConsoleInfo("1");
                    string value = queryResult.Get<string>("TotalBrackets");
                    string[] item = JsonConvert.DeserializeObject<string[]>(value);
                    value = queryResult.Get<string>("CurrentBrackets");
                    string[] item2 = JsonConvert.DeserializeObject<string[]>(value);
                    value = queryResult.Get<string>("TotalPrefix");
                    string[] item3 = JsonConvert.DeserializeObject<string[]>(value);
                    value = queryResult.Get<string>("CurrentPrefix");
                    string[] item4 = JsonConvert.DeserializeObject<string[]>(value);
                    value = queryResult.Get<string>("TotalSuffix");
                    string[] item5 = JsonConvert.DeserializeObject<string[]>(value);
                    value = queryResult.Get<string>("CurrentSuffix");
                    string[] item6 = JsonConvert.DeserializeObject<string[]>(value);

                    Tuple<IEnumerable<string>, IEnumerable<string>>[] tuples = {
                        new Tuple<IEnumerable<string>, IEnumerable<string>>(item, item2),
                        new Tuple<IEnumerable<string>, IEnumerable<string>>(item3, item4),
                        new Tuple<IEnumerable<string>, IEnumerable<string>>(item5, item6) };

                    //TShock.Log.ConsoleInfo("2 :" + tuples.Count().ToString());
                    return tuples;
                }
            }
            catch (Exception ex)
            {
                TShock.Log.Error(ex.ToString());
            }
            Tuple<IEnumerable<string>, IEnumerable<string>>[] tuple2 =
                {   new Tuple<IEnumerable<string>, IEnumerable<string>>(new string[0], new string[0]),
                    new Tuple<IEnumerable<string>, IEnumerable<string>>(new string[0], new string[0]),
                    new Tuple<IEnumerable<string>, IEnumerable<string>>(new string[0], new string[0])};
            return tuple2;
        }
        public static void Update(int userId, PlayerData data)
        {
            bool flag;
            using (QueryResult queryResult = TShock.DB.QueryReader("SELECT * FROM NewBadge WHERE Id=@0", userId))
            {
                flag = queryResult?.Read() ?? false;
            }
            try
            {
                string TotalBrackets = JsonConvert.SerializeObject(data.TotalBrackets.Select((Content x) => x.Identifier));
                string CurrentBrackets = JsonConvert.SerializeObject(data.CurrentBrackets.Select((Content x) => x.Identifier));
                string TotalPrefix = JsonConvert.SerializeObject(data.TotalPrefix.Select((Content x) => x.Identifier));
                string CurrentPrefix = JsonConvert.SerializeObject(data.CurrentPrefix.Select((Content x) => x.Identifier));
                string TotalSuffix = JsonConvert.SerializeObject(data.TotalSuffix.Select((Content x) => x.Identifier));
                string CurrentSuffix = JsonConvert.SerializeObject(data.CurrentSuffix.Select((Content x) => x.Identifier));
                TShock.DB.Query(flag ?
                    "UPDATE NewBadge SET TotalBrackets = @0, CurrentBrackets = @1,TotalPrefix = @2, CurrentPrefix = @3,TotalSuffix = @4, CurrentSuffix = @5 WHERE Id = @6;" :
                    "INSERT INTO NewBadge (TotalBrackets, CurrentBrackets, TotalPrefix, CurrentPrefix, TotalSuffix, CurrentSuffix,Id) VALUES (@0, @1, @2, @3, @4, @5, @6);", TotalBrackets, CurrentBrackets, TotalPrefix, CurrentPrefix, TotalSuffix, CurrentSuffix, userId);
            }
            catch (Exception ex)
            {
                TShock.Log.Error(ex.ToString());
            }
        }
        public static string type2Chinese(string type)
        {
            string result = "";
            switch (type)
            {
                case "prefix": result = "Ç°×º"; break;
                case "suffix": result = "ºó×º"; break;
                case "brackets": result = "À¨ºÅ"; break;
            }
            return result;
        }
    }
}
