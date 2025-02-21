using System.Collections.Generic;
using FreeSql;
using FreeSql.DataAnnotations;

namespace BossFramework.BModules
{
    public class PublicData : BaseEntity<PublicData, string>
    {
        [Column(DbType = "mediumtext")]
        public string Data { get; set; }
        public string User { get; set; }

        public static PublicData SetData(string key, object data)
        {
            if (data is null)
                throw new ArgumentNullException(nameof(data));
            var temp = new PublicData()
            {
                Id = key,
                Data = data.SerializeToJson(),
            };
            DB.DBTools.SQL.InsertOrUpdate<PublicData>().SetSource(temp).ExecuteAffrows();
            return temp;
        }
        public static string GetDataString(string key)
        {
            try
            {
                if (DB.DBTools.SQL.Select<PublicData>(key).First() is { } data)
                    return data.Data;
                else
                    return default;
            }
            catch
            {
                //Logs.Warn(ex);
                return default;
            }
        }
        public static T? GetData<T>(string key)
        {
            if (GetDataString(key) is { } result)
                return result.DeserializeJson<T>();
            return default;
        }
        public static bool ContainsData(string key)
        {
            return DB.DBTools.SQL.Select<PublicData>(key).Any();
        }
        public static bool RemoveData(string key)
        {
            return DB.DBTools.SQL.Delete<PublicData>(key).ExecuteAffrows() > 0;
        }
        public static List<string> GetKeys()
        {
            return DB.DBTools.SQL.Select<PublicData>().ToList(c => c.Id);
        }
    }
}
