using BossFramework.BAttributes;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using TerrariaApi.Server;
using TShockAPI.Hooks;

namespace BossFramework.BHooks.HookHandlers
{
    public static class ReloadHandler
    {
        public static void OnReload(ReloadEventArgs args)
        {
            Task.Run(() =>
            {
                var loaded = new List<Assembly>();
                ServerApi.Plugins.Select(p => p.PluginAssembly)
                    .Where(a => a != null)
                    .ForEach(a =>
                    {
                        if (!loaded.Contains(a))
                        {
                            a.GetTypes()
                            .ForEach(t => t.GetMethods(BindingFlags.Instance | BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public)
                            .ForEach(m =>
                            {
                                if (m.GetCustomAttribute<ReloadableAttribute>() is { })
                                    m.Invoke(BossPlugin.Instance, null);
                            }));

                            loaded.Add(a);
                        }
                    });
            });
        }
    }
}
