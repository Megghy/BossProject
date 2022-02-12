using BossFramework.BAttributes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace BossFramework.BHooks.HookHandlers
{
    public static class PostInitializeHandler
    {
        public static void OnGamePostInitialize(EventArgs args)
        {
            var auto = new Dictionary<MethodInfo, AutoPostInitAttribute>();
            Assembly.GetExecutingAssembly()
                        .GetTypes()
                        .BForEach(t => t.GetMethods(BindingFlags.Instance | BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public)
                        .BForEach(m =>
                        {
                            if (m.GetCustomAttribute<AutoPostInitAttribute>() is { } attr)
                                auto.Add(m, attr);
                        }));
            auto.OrderBy(a => a.Value.Order).BForEach(kv =>
            {
                try
                {
                    var attr = kv.Value;
                    if (attr.PreInitMessage is { } pre)
                        BLog.Info(pre);
                    kv.Key.Invoke(BPlugin.Instance, null);
                    if (attr.PostInitMessage is { } post)
                        BLog.Info(post);
                }
                catch (Exception ex)
                {
                    BLog.Error($"加载 [{kv.Key.DeclaringType!.Name}.{kv.Key.Name}] 时发生错误{Environment.NewLine}{ex}");
                }
            });
        }
    }
}
