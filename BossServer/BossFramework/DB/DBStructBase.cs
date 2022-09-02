using FreeSql;
using FreeSql.DataAnnotations;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace BossFramework.DB
{
    public abstract class DBStructBase<T> : BaseEntity<DBStructBase<T>, long> where T : DBStructBase<T>
    {
        [Column(IsPrimary = true, IsIdentity = true)]
        public override long Id { get => base.Id; set => base.Id = value; }
        public virtual void Init()
        {

        }
        public int Update<TV>(Expression<Func<T, TV>> extract, TV value, bool updateProp = true)
        {
            var prop = extract.Body
                .GetType()
                .GetProperties()
                .FirstOrDefault(p => p.Name.Contains("Member"))?
                .GetValue(extract.Body) as PropertyInfo;
            try
            {
                if (updateProp)
                    prop?.SetValue(this, value);
                return DBTools.SQL.Update<T>(this)
                    .Set(extract, value)
                    .Set(t => t.UpdateTime, DateTime.Now)
                    .ExecuteAffrows();
            }
            catch (Exception ex)
            {
                BLog.Error($"未能更新字段 {prop?.Name}: {value} => \r\n{ex}");
            }
            return 0;
        }
        public int Update<TV>(Expression<Func<T, TV>> extract)
        {
            try
            {
                var prop = extract.Body.GetType()
                    .GetProperties().FirstOrDefault(p => p.Name.Contains("Member"));
                var t = prop!.GetValue(extract.Body) as PropertyInfo;
                return DBTools.SQL.Update<T>(this)
                    .Set(extract, (TV)t?.GetValue(this)!)
                    .Set(t => t.UpdateTime, DateTime.Now)
                    .ExecuteAffrows();
            }
            catch (Exception ex)
            {
                BLog.Error($"未能更新字段\r\n{ex}");
            }
            return 0;
        }
        public int UpdateMany<TV>(params Expression<Func<T, TV>>[] extracts)
        {
            var u = DBTools.SQL.Update<T>(this);
            extracts.ForEach(e => u.Set(e));
            return u.ExecuteAffrows();
        }
    }
}
