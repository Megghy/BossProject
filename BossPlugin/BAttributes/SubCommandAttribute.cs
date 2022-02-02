using System;
using System.Reflection;

namespace BossPlugin.BAttributes
{
    /// <summary>
    /// 表示这是一个子命令
    /// </summary>
    [AttributeUsage(AttributeTargets.Method)]
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
        public string? Permission { get; set; }
        public MethodInfo? Method { get; set; }
    }
}
