using System;
using System.Reflection;

namespace BossFramework.BAttributes
{
    /// <summary>
    /// 表示这是一个子命令
    /// </summary>
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
    public class SubCommandAttribute : Attribute
    {
        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="name">子命令的名字</param>
        public SubCommandAttribute(params string[] name)
        {
            Names = name;
        }
        public string[] Names { get; set; }
        public string Permission { get; set; }
        public string Description { get; set; }
        public MethodInfo Method { get; set; }
    }
}
