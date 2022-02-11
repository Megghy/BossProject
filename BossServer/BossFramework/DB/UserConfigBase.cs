using FreeSql;
using System;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace BossFramework.DB
{
    public abstract class UserConfigBase<T> : BaseEntity<UserConfigBase<T>, string> where T : UserConfigBase<T>
    {
        public virtual void Init()
        {

        }
        public void Update<TV>(Expression<Func<T, TV>> extract, TV value, bool updateProp = true)
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
                DBTools.SQL.Update<T>(this)
                    .Set(extract, value)
                    .Set(t => t.UpdateTime, DateTime.Now)
                    .ExecuteAffrows();
            }
            catch (Exception ex)
            {
                BLog.Error($"未能更新字段 {prop?.Name}: {value} => \r\n{ex}");
            }
        }
        public void Update<TV>(Expression<Func<T, TV>> extract)
        {
            try
            {
                var prop = extract.Body.GetType()
                    .GetProperties().FirstOrDefault(p => p.Name.Contains("Member"));
                var t = prop!.GetValue(extract.Body) as PropertyInfo;
                DBTools.SQL.Update<T>(this)
                    .Set(extract, (TV)t?.GetValue(this)!)
                    .Set(t => t.UpdateTime, DateTime.Now)
                    .ExecuteAffrows();
            }
            catch (Exception ex)
            {
                BLog.Error($"未能更新字段\r\n{ex}");
            }
        }
    }
}
