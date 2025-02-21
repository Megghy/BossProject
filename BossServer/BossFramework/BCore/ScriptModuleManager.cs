using System.IO;
using BossFramework.BAttributes;
using BossFramework.BInterfaces;
using TShockAPI;

namespace BossFramework.BCore
{
    public static class ScriptModuleManager
    {
        public static IScriptModule[] ScriptModules { get; private set; }
        public static string ScriptModulesPath => Path.Combine(ScriptManager.ScriptRootPath, "Modules");
        [AutoInit]
        private static void InitModules()
        {
            BLog.DEBUG("初始化脚本模块");

            if (!Directory.Exists(ScriptModulesPath))
                Directory.CreateDirectory(ScriptModulesPath);

            LoadModules();
        }
        [Reloadable]
        private static void LoadModules()
        {
            if (ScriptModules is { Length: > 0 })
            {
                ScriptModules.ForEach(m => m.Dispose());
                BLog.Info($"- 已卸载所有模块");
            }

            ScriptModules = ScriptManager.LoadScripts<IScriptModule>(ScriptModulesPath);
            BLog.Info($"- 正在初始化模块...");
            foreach (var m in ScriptModules)
            {
                try
                {
                    m.Initialize();
                    BLog.Success($"- {m.Name} <{m.Author}> - {m.Version} 已成功初始化");
                }
                catch (Exception ex)
                {
                    BLog.Warn($"- {m.Name} 初始化失败\r\n{ex}");
                }
            }
            BLog.Success($"共加载 {ScriptModules.Length} 个模块");
        }
    }
}
