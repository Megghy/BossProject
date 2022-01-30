using BossPlugin.BAttributes;
using System.Reflection;
using TShockAPI.Hooks;

namespace BossPlugin.BNet.HookHandlers
{
    public static class ReloadHandler
    {
        public static void OnReload(ReloadEventArgs args)
        {
            Assembly.GetExecutingAssembly()
                        .GetTypes()
                        .ForEach(t => t.GetMethods(BindingFlags.Instance | BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public)
                        .ForEach(m =>
                        {
                            if (m.GetCustomAttribute<ReloadableAttribute>() is { })
                                m.Invoke(BPlugin.Instance, null);
                        }));
        }
    }
}
