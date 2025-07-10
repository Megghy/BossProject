using System.Data;
using BossFramework.BAttributes;
using MySql.Data.MySqlClient;
using Rests;
using TShockAPI;
using TShockAPI.DB;
using TShockAPI.DB.Queries;

namespace BossFramework.BCore
{
    public static class QQBot
    {
        [AutoPostInit]
        private static void Init()
        {
            QQBindingCode = new QQBindingCodeManager(TShock.DB);
            QQBinding = new QQBindingManager(TShock.DB);
            QQBan = new QQBanManager(TShock.DB);

            TShock.RestApi.Register(new SecureRestCommand("/qq/bind", QQBind, "tshock.rest.command"));
            TShock.RestApi.Register(new SecureRestCommand("/qq/find-by-qq", FindBindingByQQ, "tshock.rest.command"));
            TShock.RestApi.Register(new SecureRestCommand("/qq/find-by-account", FindBindingByAccount, "tshock.rest.command"));
            TShock.RestApi.Register(new SecureRestCommand("/qq/ban", BanQQ, "tshock.rest.command"));
            TShock.RestApi.Register(new SecureRestCommand("/qq/unban", UnbanQQ, "tshock.rest.command"));

            BLog.Success("[BOT] QQBot 已加载");
        }
        public static QQBindingCodeManager QQBindingCode;
        public static QQBindingManager QQBinding;
        public static QQBanManager QQBan;

        static RestObject RestError(string msg) => new RestObject("400") { Error = msg };
        static RestObject RestResponse(string msg) => new RestObject("200") { Response = msg };
        static RestObject RestInvalidParam(string var)
        {
            return RestError($"Missing or invalid {var} parameter");
        }
        static RestObject RestMissingParam(params string[] vars)
        {
            return RestMissingParam(string.Join(", ", vars));
        }

        [Route("/qq/bind")]
        [Permission("tshock.rest.command")]
        [Noun("qq", true, "", typeof(string))]
        [Noun("code", true, "", typeof(string))]
        [Token]
        private static object QQBind(RestRequestArgs args)
        {
            string qq = args.Parameters["qq"];
            string code = args.Parameters["code"];
            int? userIdByCode = QQBindingCode.GetUserIdByCode(code);
            if (!userIdByCode.HasValue)
            {
                return RestError("Invalid code");
            }
            UserAccount userAccountByID = TShock.UserAccounts.GetUserAccountByID(userIdByCode.Value);
            if (userAccountByID == null)
            {
                return RestError("The user of this code doesn't exist");
            }
            QQBinding.Add(qq, userIdByCode.Value);
            QQBindingCode.DeleteCode(userIdByCode.Value);
            return RestResponse("Successfully bond with account: " + userAccountByID.Name);
        }
        [Route("/qq/find-by-qq")]
        [Permission("tshock.rest.command")]
        [Noun("qq", true, "", typeof(string))]
        [Token]
        private static object FindBindingByQQ(RestRequestArgs args)
        {
            string text = args.Parameters["qq"];
            if (string.IsNullOrWhiteSpace(text))
            {
                return RestMissingParam("qq");
            }
            List<string> value = QQBinding.FindAccountsNamesByQQ(text);
            QQBanRecord banRecord = QQBan.GetBanRecord(text);
            return new RestObject
            {
                { "accountNames", value },
                { "ban", banRecord }
            };
        }
        [Route("/qq/find-by-account")]
        [Permission("tshock.rest.command")]
        [Noun("account", true, "", typeof(string))]
        [Token]
        private static object FindBindingByAccount(RestRequestArgs args)
        {
            string text = args.Parameters["account"];
            if (string.IsNullOrWhiteSpace(text))
            {
                return RestMissingParam("account");
            }
            UserAccount userAccountByName = TShock.UserAccounts.GetUserAccountByName(text);
            QQBindingRecord qQBindingRecord = null;
            QQBanRecord value = null;
            if (userAccountByName != null)
            {
                qQBindingRecord = QQBinding.GetBinding(userAccountByName.ID);
                if (qQBindingRecord != null)
                {
                    value = QQBan.GetBanRecord(qQBindingRecord.QQ);
                }
            }
            return new RestObject
            {
                { "binding", qQBindingRecord },
                { "ban", value }
            };
        }

        [Route("/qq/ban")]
        [Permission("tshock.rest.command")]
        [Noun("qq", true, "", typeof(string))]
        [Noun("reason", true, "", typeof(string))]
        [Token]
        private static object BanQQ(RestRequestArgs args)
        {
            string text = args.Parameters["qq"];
            if (string.IsNullOrWhiteSpace(text))
            {
                return RestMissingParam("qq");
            }
            string text2 = args.Parameters["reason"];
            if (string.IsNullOrWhiteSpace(text2))
            {
                return RestMissingParam("reason");
            }
            QQBan.Ban(text, text2);
            foreach (QQBindingRecord binding in QQBinding.GetBindings(text))
            {
                try
                {
                    TShock.Players.FirstOrDefault((TSPlayer p) => p?.Account?.ID == binding.UserId)?.Kick("Banned", force: true);
                }
                catch (Exception ex)
                {
                    TShock.Log.Error(ex.ToString());
                }
            }
            return RestResponse("OK");
        }
        [Route("/qq/unban")]
        [Permission("tshock.rest.command")]
        [Noun("qq", true, "", typeof(string))]
        [Token]
        private static object UnbanQQ(RestRequestArgs args)
        {
            string text = args.Parameters["qq"];
            if (string.IsNullOrWhiteSpace(text))
            {
                return RestMissingParam("qq");
            }
            QQBan.Unban(text);
            return RestResponse("OK");
        }


        #region
        public class QQBindingCodeManager
        {
            private IDbConnection database;

            public QQBindingCodeManager(IDbConnection db)
            {
                database = db;
                SqlTable table = new SqlTable("QQBindingCodes", new SqlColumn("Id", (MySqlDbType)3)
                {
                    Primary = true,
                    AutoIncrement = true
                }, new SqlColumn("UserId", (MySqlDbType)3), new SqlColumn("Code", (MySqlDbType)752), new SqlColumn("ExpireAt", (MySqlDbType)8));
                IQueryBuilder provider;
                if (db.GetSqlType() != SqlType.Sqlite)
                {
                    IQueryBuilder queryBuilder = new MysqlQueryBuilder();
                    provider = queryBuilder;
                }
                else
                {
                    IQueryBuilder queryBuilder = new SqliteQueryBuilder();
                    provider = queryBuilder;
                }
                SqlTableCreator sqlTableCreator = new SqlTableCreator(db, provider);
                try
                {
                    sqlTableCreator.EnsureTableStructure(table);
                }
                catch (DllNotFoundException)
                {
                    Console.WriteLine("Possible problem with your database - is Sqlite3.dll present?");
                    throw new Exception("Could not find a database library (probably Sqlite3.dll)");
                }
            }

            public int? GetUserIdByCode(string code)
            {
                using (QueryResult queryResult = database.QueryReader("SELECT * FROM QQBindingCodes WHERE Code=@0", code))
                {
                    if (queryResult.Read())
                    {
                        int num = queryResult.Get<int>("Id");
                        long num2 = queryResult.Get<long>("ExpireAt");
                        if (DateTime.UtcNow.Ticks >= num2)
                        {
                            database.Query("DELETE FROM QQBindingCodes WHERE Id=@0;", num);
                            return null;
                        }
                        return queryResult.Get<int>("UserId");
                    }
                }
                return null;
            }

            public string? GetCode(int userId)
            {
                using (QueryResult queryResult = database.QueryReader("SELECT * FROM QQBindingCodes WHERE UserId=@0", userId))
                {
                    if (queryResult.Read())
                    {
                        int id = queryResult.Get<int>("Id");
                        long num = queryResult.Get<long>("ExpireAt");
                        if (DateTime.UtcNow.Ticks >= num)
                        {
                            DeleteCodeById(id);
                            return null;
                        }
                        return queryResult.Get<string>("Code");
                    }
                }
                return null;
            }

            public string Create(int userId)
            {
                string code = GetCode(userId);
                if (code != null)
                {
                    return code;
                }
                code = GenerateNewCode();
                database.Query("INSERT INTO QQBindingCodes (UserId, Code, ExpireAt) VALUES (@0, @1, @2);", userId, code, (DateTime.UtcNow + new TimeSpan(0, 10, 0)).Ticks);
                return code;
            }

            public string GenerateNewCode()
            {
                Random random = new Random();
                string text = null;
                while (text == null)
                {
                    string text2 = random.Next(10000000, 99999999).ToString();
                    using QueryResult queryResult = database.QueryReader("SELECT * FROM QQBindingCodes WHERE Code=@0", text2);
                    if (!queryResult.Read())
                    {
                        text = text2;
                        break;
                    }
                }
                return text;
            }

            public void DeleteCode(string code)
            {
                database.Query("DELETE FROM QQBindingCodes WHERE Code=@0;", code);
            }

            public void DeleteCode(int userId)
            {
                database.Query("DELETE FROM QQBindingCodes WHERE UserId=@0;", userId);
            }

            public void DeleteCodeById(int id)
            {
                database.Query("DELETE FROM QQBindingCodes WHERE Id=@0;", id);
            }
        }
        public class QQBindingManager
        {
            private IDbConnection database;

            public QQBindingManager(IDbConnection db)
            {
                database = db;
                SqlTable table = new SqlTable("QQBindings", new SqlColumn("Id", (MySqlDbType)3)
                {
                    Primary = true,
                    AutoIncrement = true
                }, new SqlColumn("QQ", (MySqlDbType)752), new SqlColumn("UserId", (MySqlDbType)3), new SqlColumn("CreatedAt", (MySqlDbType)8));
                IQueryBuilder provider;
                if (db.GetSqlType() != SqlType.Sqlite)
                {
                    IQueryBuilder queryBuilder = new MysqlQueryBuilder();
                    provider = queryBuilder;
                }
                else
                {
                    IQueryBuilder queryBuilder = new SqliteQueryBuilder();
                    provider = queryBuilder;
                }
                SqlTableCreator sqlTableCreator = new SqlTableCreator(db, provider);
                try
                {
                    sqlTableCreator.EnsureTableStructure(table);
                }
                catch (DllNotFoundException)
                {
                    Console.WriteLine("Possible problem with your database - is Sqlite3.dll present?");
                    throw new Exception("Could not find a database library (probably Sqlite3.dll)");
                }
            }

            public List<string> FindAccountsNamesByQQ(string qq)
            {
                List<string> list = new List<string>();
                using QueryResult queryResult = database.QueryReader("SELECT * FROM QQBindings WHERE QQ=@0", qq);
                while (queryResult.Read())
                {
                    int num = queryResult.Get<int>("UserId");
                    QueryResult queryResult2 = database.QueryReader("SELECT Username FROM Users WHERE ID=@0", num);
                    if (queryResult2.Read())
                    {
                        list.Add(queryResult2.Get<string>("Username"));
                    }
                }
                return list;
            }

            public QQBindingRecord GetBinding(int userId)
            {
                QQBindingRecord qQBindingRecord = null;
                using (QueryResult queryResult = database.QueryReader("SELECT * FROM QQBindings WHERE UserId=@0", userId))
                {
                    if (queryResult.Read())
                    {
                        qQBindingRecord = new QQBindingRecord();
                        qQBindingRecord.Id = queryResult.Get<int>("Id");
                        qQBindingRecord.QQ = queryResult.Get<string>("QQ");
                        qQBindingRecord.UserId = queryResult.Get<int>("UserId");
                        qQBindingRecord.CreatedAt = queryResult.Get<long>("CreatedAt");
                    }
                }
                return qQBindingRecord;
            }

            public List<QQBindingRecord> GetBindings(string qq)
            {
                List<QQBindingRecord> list = new List<QQBindingRecord>();
                using QueryResult queryResult = database.QueryReader("SELECT * FROM QQBindings WHERE QQ=@0", qq);
                while (queryResult.Read())
                {
                    QQBindingRecord qQBindingRecord = new QQBindingRecord();
                    qQBindingRecord.Id = queryResult.Get<int>("Id");
                    qQBindingRecord.QQ = queryResult.Get<string>("QQ");
                    qQBindingRecord.UserId = queryResult.Get<int>("UserId");
                    qQBindingRecord.CreatedAt = queryResult.Get<long>("CreatedAt");
                    list.Add(qQBindingRecord);
                }
                return list;
            }

            public void Add(string qq, int userId)
            {
                database.Query("INSERT INTO QQBindings (QQ, UserId, CreatedAt) VALUES (@0, @1, @2);", qq, userId, DateTime.UtcNow.Ticks);
            }

            public void Delete(int userId)
            {
                database.Query("DELETE FROM QQBindings WHERE UserId=@0;", userId);
            }
        }
        public class QQBanManager
        {
            private IDbConnection database;

            public QQBanManager(IDbConnection db)
            {
                database = db;
                SqlTable table = new SqlTable("QQBans", new SqlColumn("Id", (MySqlDbType)3)
                {
                    Primary = true,
                    AutoIncrement = true
                }, new SqlColumn("QQ", (MySqlDbType)752), new SqlColumn("Reason", (MySqlDbType)752), new SqlColumn("CreatedAt", (MySqlDbType)8));
                IQueryBuilder provider;
                if (db.GetSqlType() != SqlType.Sqlite)
                {
                    IQueryBuilder queryBuilder = new MysqlQueryBuilder();
                    provider = queryBuilder;
                }
                else
                {
                    IQueryBuilder queryBuilder = new SqliteQueryBuilder();
                    provider = queryBuilder;
                }
                SqlTableCreator sqlTableCreator = new SqlTableCreator(db, provider);
                try
                {
                    sqlTableCreator.EnsureTableStructure(table);
                }
                catch (DllNotFoundException)
                {
                    Console.WriteLine("Possible problem with your database - is Sqlite3.dll present?");
                    throw new Exception("Could not find a database library (probably Sqlite3.dll)");
                }
            }

            public QQBanRecord? GetBanRecord(string qq)
            {
                using (QueryResult queryResult = database.QueryReader("SELECT * FROM QQBans WHERE QQ=@0", qq))
                {
                    if (queryResult.Read())
                    {
                        return new QQBanRecord
                        {
                            Id = queryResult.Get<int>("Id"),
                            QQ = queryResult.Get<string>("QQ"),
                            Reason = queryResult.Get<string>("Reason"),
                            CreatedAt = queryResult.Get<long>("CreatedAt")
                        };
                    }
                }
                return null;
            }

            public void Ban(TSPlayer player, string reason)
            {
                UserAccount account = player.Account;
                if (account != null)
                {
                    Ban(account.ID, reason);
                }
            }

            public void Ban(int userId, string reason)
            {
                QQBindingRecord binding = QQBinding.GetBinding(userId);
                if (binding != null)
                {
                    Ban(binding, reason);
                }
            }

            public void Ban(QQBindingRecord binding, string reason)
            {
                if (binding != null)
                {
                    Ban(binding.QQ, reason);
                }
            }

            public void Ban(string qq, string reason)
            {
                database.Query("INSERT INTO QQBans (QQ, Reason, CreatedAt) VALUES (@0, @1, @2);", qq, reason, DateTime.UtcNow.Ticks);
            }

            public void Unban(string qq)
            {
                database.Query("DELETE FROM QQBans WHERE QQ=@0;", qq);
            }
        }
        public class QQBanRecord
        {
            public int Id { get; set; }

            public string QQ { get; set; }

            public string Reason { get; set; }

            public long CreatedAt { get; set; }
        }
        public class QQBindingRecord
        {
            public int Id { get; set; }

            public string QQ { get; set; }

            public int UserId { get; set; }

            public long CreatedAt { get; set; }
        }

        #endregion
    }
}
