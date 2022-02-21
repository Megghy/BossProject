using BossFramework.BAttributes;
using System.Reflection;
using TShockAPI.Hooks;

namespace BossFramework.BHooks.HookHandlers
{
    public static class ReloadHandler
    {
        public static void OnReload(ReloadEventArgs args)
        {
            Assembly.GetExecutingAssembly()
                        .GetTypes()
                        .ForEach(t => t.GetMethods(BindingFlags.Instance | BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.FlattenHierarchy)
                        .ForEach(m =>
                        {
                            if (m.GetCustomAttribute<ReloadableAttribute>() is { } && m.DeclaringType.Name != "BaseConfig`1")
                                m.Invoke(BossPlugin.Instance, null);
                        }));
        }
    }
}
