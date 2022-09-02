using BossFramework.BAttributes;
using FreeSql;
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
        public static T[] GetAll<T>() where T : DBStructBase<T>
        {
            var result = SQL.Select<T>().ToList();
            result.ForEach(r => r.Init());
            return result.ToArray();
        }
        public static T[] GetAll<T>(Expression<Func<T, bool>> extract) where T : DBStructBase<T>
        {
            var result = SQL.Select<T>().Where(extract).ToList();
            result.ForEach(r => r.Init());
            return result.ToArray();
        }
        public static T Get<T>(Expression<Func<T, bool>> extract) where T : DBStructBase<T>
        {
            var result = SQL.Select<T>().Where(extract).First();
            result.Init();
            return result;
        }
        /// <summary>
        /// 返回自增值
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="target"></param>
        /// <returns></returns>
        public static T Insert<T>(T target) where T : DBStructBase<T>
        {
            target.Id = SQL.Insert(target).ExecuteIdentity();
            return target;
        }
        public static int InsertOrUpdate<T>(T target) where T : DBStructBase<T>
        {
            return SQL.InsertOrUpdate<T>().SetSource(target).ExecuteAffrows();
        }
        public static int Delete<T>(T target) where T : DBStructBase<T>
            => SQL.Delete<T>(target).ExecuteAffrows();
        public static int Delete<T>(long id) where T : DBStructBase<T>
            => SQL.Delete<T>().Where(t => t.Id == id).ExecuteAffrows();
        public static int Delete<T>(Expression<Func<T, bool>> extract) where T : DBStructBase<T>
            => SQL.Delete<T>().Where(extract).ExecuteAffrows();

        public static bool Exist<T>(Expression<Func<T, bool>> extract) where T : DBStructBase<T>
            => SQL.Select<T>().Any(extract);
        public static T GetNonInsert<T>(Expression<Func<T, bool>> extract) where T : DBStructBase<T>
        {
            var result = SQL.Select<T>().Where(extract).First();
            result?.Init();
            return result;
        }
        public static T GetNonInsert<T>(int id) where T : DBStructBase<T>
            => GetNonInsert<T>(target => target.Id == id);
        public static T Get<T>(int id) where T : DBStructBase<T>
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
