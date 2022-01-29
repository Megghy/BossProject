using System;

namespace BossPlugin.BAttributes
{
    /// <summary>
    /// 表示此子命令需要权限才能使用
    /// </summary>
    [AttributeUsage(AttributeTargets.Method)]
    public class NeedPermissionAttribute : Attribute
    {
        public string[] Perms { get; set; }

        public NeedPermissionAttribute(params string[] perms)
        {
            Perms = perms;
        }
    }
}
