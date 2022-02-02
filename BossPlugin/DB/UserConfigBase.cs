using LinqToDB;
using LinqToDB.Data;
using LinqToDB.DataProvider;
using LinqToDB.DataProvider.MySql;
using LinqToDB.DataProvider.SQLite;
using LinqToDB.Mapping;
using System;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using TShockAPI;
using TShockAPI.DB;

namespace BossPlugin.DB
{
    public abstract class ConfigBase<T> where T : ConfigBase<T>
    {
        public class Context : DataConnection
        {
            public ITable<T> Config => GetTable<T>();

            private static IDataProvider GetProvider()
            {
                return TShock.DB.GetSqlType() switch
                {
                    SqlType.Mysql => new MySqlDataProvider("MySqlConnector"),
                    SqlType.Sqlite => new SQLiteDataProvider(string.Empty),
                    _ => null,
                };
            }

            public Context(string tableName) : base(GetProvider(), TShock.DB.ConnectionString)
            {
                this.CreateTable<T>(tableName, tableOptions: TableOptions.CreateIfNotExists);
            }
        }
        internal static Context GetContext(string tableName) => new(tableName);
    }
    public abstract class UserConfigBase<T> : ConfigBase<T> where T : UserConfigBase<T>
    {
        public UserConfigBase()
        {

        }
        public new class Context : ConfigBase<T>.Context
        {
            public void InsertDirect(T t)
            {
                t.LastUpdate = DateTime.Now;
                this.Insert(t);
            }
            public IQueryable<T> Get(string id)
            {
                if (!Config.Any(t => t.ID == id))
                {
                    var r = Activator.CreateInstance<T>();
                    r.ID = id;
                    r.Init();
                    InsertDirect(r);
                }
                return GetNonInsert(id);
            }
            public IQueryable<T> GetNonInsert(string id)
            {
                var result = Config.Where(t => t.ID == id);
                result.ForEach(r => r.Init());
                return result;
            }
            public Context(string tableName) : base(tableName)
            {
            }
        }

        internal new static Context GetContext(string tableName) => new(tableName);

        public virtual void Init()
        {

        }
        public void Update<TV>(Expression<Func<T, TV>> extract, TV value, bool updateProp = true)
        {
            var prop = extract.Body
                .GetType()
                .GetProperties()
                .FirstOrDefault(p => p.Name.Contains("Member"))
                .GetValue(extract.Body) as PropertyInfo;
            try
            {
                using var query = GetContext(typeof(T).Name);
                if (updateProp)
                    prop.SetValue(this, value);
                using var temp = new DisposableQuery<T>(query.Get(ID), query);
                temp.Set(extract, value)
                    .Set(b => b.LastUpdate, DateTime.Now)
                    .Update();
            }
            catch (Exception ex)
            {
                BLog.Error($"未能更新字段 {prop.Name}: {value} => \r\n{ex}");
            }
        }
        public void Update<TV>(Expression<Func<T, TV>> extract)
        {
            try
            {
                using var query = GetContext(typeof(T).Name);
                var target = query.Get(ID);
                var prop = extract.Body.GetType()
                    .GetProperties().FirstOrDefault(p => p.Name.Contains("Member"));
                var t = prop.GetValue(extract.Body) as PropertyInfo;
                using var temp = new DisposableQuery<T>(query.Get(ID), query);
                temp.Set(extract, (TV)t.GetValue(this))
                    .Set(b => b.LastUpdate, DateTime.Now)
                    .Update();
            }
            catch (Exception ex)
            {
                BLog.Error($"未能更新字段\r\n{ex}");
            }
        }
        [PrimaryKey, NotNull]
        public string ID { get; set; }
        public DateTime LastUpdate { get; set; }
    }
}
