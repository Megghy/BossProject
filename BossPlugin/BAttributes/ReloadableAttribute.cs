using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BossPlugin.BAttributes
{
    /// <summary>
    /// 将在使用/reload时调用的方法
    /// </summary>
    [AttributeUsage(AttributeTargets.Method)]
    public class ReloadableAttribute : Attribute
    {
    }
}
