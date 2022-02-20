using System;

namespace BossFramework.BAttributes
{
    [AttributeUsage(AttributeTargets.Method)]
    public class SimpleTimerAttribute : Attribute
    {
        /// <summary>
        /// 单位为s
        /// </summary>
        public int Time { get; set; } = 30;
        public bool CallOnRegister { get; set; } = true;
    }
}
