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
        public static T Insert<T>(T target) where T : UserConfigBase<T>
        {
            try
            {
                SQL.Insert(target).ExecuteAffrows();
            }
            catch (Exception ex)
            {
                BLog.Error($"未能向表 {typeof(T).Name} 中添加 {target.Id}: {ex}");
            }
            return target;
        }
        public static int Delete<T>(T target) where T : UserConfigBase<T>
        {
            try
            {
                return SQL.Delete<T>(target).ExecuteDeleted().Count;
            }
            catch (Exception ex)
            {
                BLog.Error($"未能从表 {typeof(T).Name} 中移除 {target.Id}: {ex}");
                return -1;
            }
        }
        public static T GetNonInsert<T>(int id) where T : UserConfigBase<T>
        {
            try
            {
                return SQL.Select<T>().Where(r => r.Id == id).First();
            }
            catch (Exception ex)
            {
                BLog.Error($"未能从数据库获取对象: {ex}");
                return null;
            }
        }
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
