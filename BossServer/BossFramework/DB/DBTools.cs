using BossFramework.BAttributes;
using FreeSql;
using System;
using System.Linq.Expressions;

namespace BossFramework.DB
{
    /// <summary>
    /// 数据库工具类
    /// </summary>
    public static class DBTools
    {
        public static IFreeSql SQL;
        [AutoInit(order: 0)]
        private static void InitDB()
        {
            SQL = new FreeSqlBuilder()
            .UseConnectionString(DataType.MySql, TShockAPI.TShock.DB.ConnectionString)
            .UseAutoSyncStructure(true)
            .Build();
            SQL.UseJsonMap();
        }
        public static T[] GetAll<T>() where T : UserConfigBase<T>
        {
            var result = SQL.Select<T>().ToList();
            result.ForEach(r => r.Init());
            return result.ToArray();
        }
        public static T[] GetAll<T>(Expression<Func<T, bool>> extract) where T : UserConfigBase<T>
        {
            var result = SQL.Select<T>().Where(extract).ToList();
            result.ForEach(r => r.Init());
            return result.ToArray();
        }
        public static T Get<T>(Expression<Func<T, bool>> extract) where T : UserConfigBase<T>
        {
            var result = SQL.Select<T>().Where(extract).First();
            result.Init();
            return result;
        }
        public static int Insert<T>(T target) where T : UserConfigBase<T>
            => SQL.Insert(target).ExecuteAffrows();

        public static int Delete<T>(T target) where T : UserConfigBase<T>
            => SQL.Delete<T>(target).ExecuteAffrows();
        public static int Delete<T>(int id) where T : UserConfigBase<T>
            => SQL.Delete<T>().Where(t => t.Id == id).ExecuteAffrows();
        public static int Delete<T>(Expression<Func<T, bool>> extract) where T : UserConfigBase<T>
            => SQL.Delete<T>().Where(extract).ExecuteAffrows();

        public static bool Exist<T>(Expression<Func<T, bool>> extract) where T : UserConfigBase<T>
            => SQL.Select<T>().Any(extract);
        public static T GetNonInsert<T>(int id) where T : UserConfigBase<T>
            => SQL.Select<T>().Where(r => r.Id == id).First();
        public static T Get<T>(int id) where T : UserConfigBase<T>
        {
            var result = GetNonInsert<T>(id);
            if (result == null)
            {
                var r = Activator.CreateInstance<T>();
                r.Init();
                Insert(r);
                return r;
            }
            else
            {
                result.Init();
                return result;
            }
        }
    }
}
