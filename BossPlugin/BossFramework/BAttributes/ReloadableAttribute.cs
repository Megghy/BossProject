namespace BossFramework.BAttributes
{
    /// <summary>
    /// 将在使用/reload时调用的方法
    /// </summary>
    [AttributeUsage(AttributeTargets.Method)]
    public class ReloadableAttribute : Attribute
    {
    }
}
