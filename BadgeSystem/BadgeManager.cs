using System.Data;
using MySqlConnector;
using Newtonsoft.Json;
using TShockAPI;
using TShockAPI.DB;

namespace BadgeSystem
{
    internal sealed class BadgeManager
	{
		private readonly IDbConnection _database;

		public BadgeManager(IDbConnection conn)
		{
			_database = conn;
			SqlTable table = new SqlTable("Badges", new SqlColumn("Id", MySqlDbType.Int32)
			{
				Primary = true
			}, new SqlColumn("Total", MySqlDbType.Text), new SqlColumn("Current", MySqlDbType.Text));
			IDbConnection database = _database;
			IQueryBuilder provider;
			if (_database.GetSqlType() != SqlType.Sqlite)
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

		public Tuple<IEnumerable<string>, IEnumerable<string>> Load(int userId)
		{
			try
			{
				using QueryResult queryResult = _database.QueryReader("SELECT * FROM Badges WHERE Id=@0", userId);
				if (queryResult?.Read() ?? false)
				{
					string value = queryResult.Get<string>("Total");
					string[] item = JsonConvert.DeserializeObject<string[]>(value);
					value = queryResult.Get<string>("Current");
					string[] item2 = JsonConvert.DeserializeObject<string[]>(value);
					return new Tuple<IEnumerable<string>, IEnumerable<string>>(item, item2);
				}
			}
			catch (Exception ex)
			{
				TShock.Log.Error(ex.ToString());
			}
			return new Tuple<IEnumerable<string>, IEnumerable<string>>(new string[0], new string[0]);
		}

		public void Update(int userId, PlayerData data)
		{
			bool flag;
			using (QueryResult queryResult = _database.QueryReader("SELECT * FROM Badges WHERE Id=@0", userId))
			{
				flag = queryResult?.Read() ?? false;
			}
			try
			{
				string text = JsonConvert.SerializeObject(data.Total.Select((Badge x) => x.Identifier));
				string text2 = JsonConvert.SerializeObject(data.Current.Select((Badge x) => x.Identifier));
				_database.Query(flag ? "UPDATE Badges SET Total = @0, Current = @1 WHERE Id = @2;" : "INSERT INTO Badges (Total, Current, Id) VALUES (@0, @1, @2);", text, text2, userId);
			}
			catch (Exception ex)
			{
				TShock.Log.Error(ex.ToString());
			}
		}
	}
}
