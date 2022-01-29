using BossPlugin.BAttributes;
using CSScriptLibrary;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Terraria;

namespace BossPlugin.BCore
{
    public static class ScriptManager
    {
        public static string ScriptRootPath => Path.Combine(BInfo.FilePath, "Scripts");
        public static string MiniGameScriptPath => Path.Combine(ScriptRootPath, "MiniGames");
        private static Assembly _currentAssembly => AppDomain.CurrentDomain.GetAssemblies().First(a => a.FullName.StartsWith("BossPlugin"));
        /// <summary>
        /// 加载指定脚本文件
        /// </summary>
        /// <typeparam name="T">尝试加载为指定类型</typeparam>
        /// <param name="filePath">文件路径</param>
        /// <returns></returns>
        /// <exception cref="FileNotFoundException"></exception>
        public static T LoadSingleScript<T>(string filePath) where T : class
        {
            if (!File.Exists(filePath))
                throw new FileNotFoundException($"脚本文件 {filePath} 不存在");
            try
            {
                var code = File.ReadAllText(filePath);
                return CSScript.MonoEvaluator.ReferenceAssembly(_currentAssembly)
                    .ReferenceAssembliesFromCode(code)
                    .LoadCode<T>(code);
            }
            catch (Exception ex)
            {
                BLog.Error($"脚本加载失败: {ex}");
                return null;
            }
        }
        /// <summary>
        /// 加载指定路径下的所有脚本文件
        /// </summary>
        /// <typeparam name="T">指定类型</typeparam>
        /// <param name="path">路径</param>
        /// <returns>脚本</returns>
        /// <exception cref="DirectoryNotFoundException"></exception>
        public static T[] LoadScripts<T>(string path) where T : class
        {
            if (!Directory.Exists(path))
                throw new DirectoryNotFoundException($"路径 {path} 不存在");
            var scripts = new List<T>();
            Directory.GetFiles(path, "*.cs").ForEach(f =>
            {
                try
                {
                    scripts.Add(LoadSingleScript<T>(f));
                }
                catch (Exception ex)
                {
                    BLog.Error(ex);
                }
            });
            return scripts.ToArray();
        }
        [AutoInit(order: 10)]
        private static void CheckPath()
        {
            if (!Directory.Exists(ScriptRootPath))
                Directory.CreateDirectory(ScriptRootPath);
            if (!Directory.Exists(MiniGameScriptPath))
                Directory.CreateDirectory(MiniGameScriptPath);
        }
    }
}
