using BossFramework.BAttributes;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using System.Timers;
using TerrariaApi.Server;

namespace BossFramework.BModules
{
    public static class SimpleTimerManager
    {
        private static readonly Dictionary<MethodInfo, SimpleTimerAttribute> _timers = new();
        private static long time = 0;
        [AutoInit]
        public static void RegisterAll()
        {
            BLog.DEBUG($"初始化计时器");
            Timer temp = new()
            {
                Interval = 1000,
                AutoReset = true,
            };
            temp.Elapsed += (_, _) =>
            {
                if (time != 0)
                    _timers.Where(timer => time % timer.Value.Time == 0)
                    .ForEach(timer =>
                    {
                        Task.Run(() => timer.Key.Invoke(null, null));
                    });
                time++;
            };
            var loaded = new List<Assembly>();
            ServerApi.Plugins.Select(p => p.PluginAssembly)
                .Where(a => a != null)
                .ForEach(a =>
                {
                    if (!loaded.Contains(a))
                    {
                        a.GetTypes()
                            .ForEach(t => t.GetMethods(BindingFlags.Instance | BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public)
                            .Where(m => m.GetCustomAttributes(true).FirstOrDefault(a => a is SimpleTimerAttribute) != null)
                            .ForEach(m =>
                            {
                                var attr = m.GetCustomAttributes(true).FirstOrDefault(a => a is SimpleTimerAttribute) as SimpleTimerAttribute;
                                _timers.Add(m, attr);
                            }));

                        loaded.Add(a);
                    }
                });
            temp.Start();
            _timers.Where(timer => timer.Value.CallOnRegister).ForEach(timer => Task.Run(() => timer.Key.Invoke(null, null)));
        }
    }
}
