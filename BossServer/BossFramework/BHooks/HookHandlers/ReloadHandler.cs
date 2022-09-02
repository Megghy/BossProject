using BossFramework.BAttributes;
using System.Reflection;
using TShockAPI;
using TShockAPI.Hooks;

namespace BossFramework.BHooks.HookHandlers
{
    public static class ReloadHandler
    {
        public static TSPlayer Caller { get; private set; }
        public static void OnReload(ReloadEventArgs args)
        {
            if (Caller is not null)
                return;
            Caller = args.Player;
            try
            {
                Assembly.GetExecutingAssembly()
                        .GetTypes()
                        .ForEach(t => t.GetMethods(BindingFlags.Instance | BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.FlattenHierarchy)
                        .ForEach(m =>
                        {
                            if (m.GetCustomAttribute<ReloadableAttribute>() is { } && m.ReflectedType.Name != "BaseConfig`1")
                                m.Invoke(BossPlugin.Instance, null);
                        }));
            }
            catch (Exception ex)
            {
                Caller?.SendInfoMessage(ex.Message);
            }
            finally
            {
                Caller = null;
            }
        }
    }
}
