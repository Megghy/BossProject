using System;

namespace BossPlugin.BAttributes
{
    /// <summary>
    /// 将在启动时自动调用的方法
    /// </summary>
    [AttributeUsage(AttributeTargets.Method)]
    public class AutoInitAttribute : Attribute
    {
        public AutoInitAttribute(string preMsg = null, string postMsg = null, int order = 100)
        {
            PreInitMessage = preMsg;
            PostInitMessage = postMsg;
            Order = order;
        }
        public int Order { get; private set; }
        public string PreInitMessage { get; private set; }
        public string PostInitMessage { get; private set; }
    }
}
