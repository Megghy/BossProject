using LinqToDB;
using System;
using System.Linq;

namespace BossPlugin.DB
{
    /// <summary>
    /// 数据库工具类
    /// </summary>
    public static class DBTools
    {
        public static UserConfigBase<T>.Context UserContext<T>(string? tableName = null) where T : UserConfigBase<T> => UserConfigBase<T>.GetContext(tableName);
        public static ConfigBase<T>.Context Context<T>(string? tableName = null) where T : ConfigBase<T> => ConfigBase<T>.GetContext(tableName);
        public static T Insert<T>(T target, string? tableName = null) where T : UserConfigBase<T>
        {
            tableName ??= typeof(T).Name;
            try
            {
                UserConfigBase<T>.GetContext(tableName).InsertDirect(target);
            }
            catch (Exception ex)
            {
                BLog.Error($"未能向表 {tableName} 中添加 {target.ID}: {ex}");
            }
            return target;
        }
        public static int Delete<T>(T target, string? tableName = null) where T : UserConfigBase<T>
        {
            tableName ??= typeof(T).Name;
            try
            {
                return UserConfigBase<T>.GetContext(tableName).Delete(target);
            }
            catch (Exception ex)
            {
                BLog.Error($"未能从表 {tableName} 中移除 {target.ID}: {ex}");
                return -1;
            }
        }
        public static DisposableQuery<T> Get<T>(int id, string? tableName = null) where T : UserConfigBase<T>
            => Get<T>(id.ToString(), tableName);
        public static DisposableQuery<T>? GetNonInsert<T>(string id, string? tableName = null) where T : UserConfigBase<T>
        {
            try
            {
                var context = UserConfigBase<T>.GetContext(tableName ?? typeof(T).Name);
                return new DisposableQuery<T>(context.GetNonInsert(id.ToString()), context);
            }
            catch (Exception ex)
            {
                BLog.Error($"未能从数据库获取对象: {ex}");
                return null;
            }
        }
        public static T? GetSingleNonInsert<T>(string id, string? tableName = null) where T : UserConfigBase<T>
            => GetNonInsert<T>(id, tableName)?.FirstOrDefault();
        public static DisposableQuery<T> Get<T>(string id, string? tableName = null) where T : UserConfigBase<T>
        {
            var context = UserConfigBase<T>.GetContext(tableName ?? typeof(T).Name);
            return new DisposableQuery<T>(context.Get(id.ToString()), context);
        }
        public static T GetSingle<T>(string id, string? tableName = null) where T : UserConfigBase<T>
            => Get<T>(id, tableName).First();
    }
}
