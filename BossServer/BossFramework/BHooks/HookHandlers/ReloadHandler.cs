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
                        .BForEach(t => t.GetMethods(BindingFlags.Instance | BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public)
                        .BForEach(m =>
                        {
                            if (m.GetCustomAttribute<ReloadableAttribute>() is { })
                                m.Invoke(BossPlugin.Instance, null);
                        }));
        }
    }
}
