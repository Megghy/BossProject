using BossFramework.BAttributes;
using BossFramework.BModels;
using CSScriptLib;
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
                        .ForEach(t => t.GetMethods(BindingFlags.Instance | BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public)
                        .ForEach(m =>
                        {
                            if (m.GetCustomAttribute<AutoPostInitAttribute>() is { } attr)
                                auto.Add(m, attr);
                        }));
            auto.OrderBy(a => a.Value.Order).ForEach(kv =>
            {
                try
                {
                    var attr = kv.Value;
                    if (attr.PreInitMessage is { } pre)
                        BLog.Info(pre);
                    kv.Key.Invoke(BossPlugin.Instance, null);
                    if (attr.PostInitMessage is { } post)
                        BLog.Info(post);
                }
                catch (Exception ex)
                {
                    BLog.Error($"加载 [{kv.Key.DeclaringType!.Name}.{kv.Key.Name}] 时发生错误{Environment.NewLine}{ex}");
                }
            });

            CSScript.Evaluator.ReferenceDomainAssemblies();
            CSScript.Evaluator.ReferenceAssemblyOf<BEventArgs.BaseEventArgs>();
            CSScript.Evaluator.ReferenceAssemblyByNamespace("BossFramework.BModels");
        }
    }
}
