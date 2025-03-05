namespace BossFramework.BAttributes
{
    [AttributeUsage(AttributeTargets.Method)]
    public class SimpleTimerAttribute : Attribute
    {
        public SimpleTimerAttribute(int time = 30, bool callOnRegister = true) { Time = time; CallOnRegister = callOnRegister; }
        /// <summary>
        /// 单位为s
        /// </summary>
        public int Time { get; set; } = 30;
        public bool CallOnRegister { get; set; } = true;
    }
}
