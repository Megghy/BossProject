using System.Diagnostics;
using System.Text.RegularExpressions;
using TShockAPI;

namespace BossFramework
{
    public static class BLog
    {
        public static void Log(object text, bool save = true)
        {
            LogDirect(text, "Log", ConsoleColor.Gray, save);
        }
        public static void Info(object text, bool save = true)
        {
            LogDirect(text, "Info", ConsoleColor.Yellow, save);
        }
        public static void Error(object text, bool save = true)
        {
            LogDirect(text, "Error", ConsoleColor.Red, save);
        }
        public static void Warn(object text, bool save = true)
        {
            LogDirect(text, "Warn", ConsoleColor.DarkYellow, save);
        }
        public static void Success(object text, bool save = true)
        {
            LogDirect(text, "Success", ConsoleColor.Green, save);
        }

        public static void DEBUG(object text)
        {
            if (!BConfig.Instance.DebugInfo)
                return;
            var callerInfo = GetMeaningfulCaller(0);
            var debugText = $"[{callerInfo}] <DEBUG> {text}";
            Console.ForegroundColor = ConsoleColor.DarkCyan;
            Console.WriteLine(debugText);
            Console.ResetColor();
            BInfo.OnlinePlayers.Where(p => p.TSPlayer?.HasPermission("boss.admin.debug") == true)
                .ForEach(p => BUtils.SendMsg(p.TSPlayer, debugText, new(130, 200, 200)));
        }

        /// <summary>
        /// 获取有意义的调用者信息，智能处理lambda表达式
        /// </summary>
        /// <param name="skipFrames">要跳过的帧数</param>
        /// <returns>格式化的调用者信息</returns>
        private static string GetMeaningfulCaller(int skipFrames)
        {
            var stackTrace = new StackTrace(true);
            var frames = stackTrace.GetFrames();

            if (frames == null || frames.Length <= 1)
                return "Unknown";

            // 从指定位置开始查找有意义的调用者
            for (int i = skipFrames + 1; i < frames.Length; i++)
            {
                var frame = frames[i];
                var method = frame.GetMethod();
                if (method == null) continue;

                var declaringType = method.DeclaringType;
                if (declaringType == null) continue;

                var typeName = declaringType.Name;
                var methodName = method.Name;

                // 跳过BLog类内部的所有方法调用
                if (declaringType == typeof(BLog))
                    continue;

                // 检查是否是编译器生成的lambda或匿名方法
                if (IsCompilerGenerated(typeName, methodName))
                {
                    // 尝试从编译器生成的名称中提取原始信息
                    var extractedInfo = ExtractOriginalMethodInfo(typeName, methodName, declaringType);
                    if (!string.IsNullOrEmpty(extractedInfo))
                    {
                        return $"{extractedInfo} (Lambda)";
                    }
                    continue; // 继续查找更上层的调用者
                }

                // 返回正常的方法信息
                return $"{typeName}.{methodName}";
            }

            return "Unknown";
        }

        /// <summary>
        /// 检查是否是编译器生成的方法
        /// </summary>
        private static bool IsCompilerGenerated(string typeName, string methodName)
        {
            // 检查常见的编译器生成模式
            return typeName.Contains("<>") ||
                   typeName.Contains("__DisplayClass") ||
                   methodName.Contains("<") ||
                   methodName.Contains("b__") ||
                   methodName.Contains("d__"); // async方法
        }

        /// <summary>
        /// 从编译器生成的类型名中提取原始方法信息
        /// </summary>
        private static string ExtractOriginalMethodInfo(string typeName, string methodName, Type declaringType)
        {
            try
            {
                // 尝试从嵌套类型的外部类型获取信息
                var outerType = declaringType.DeclaringType;
                if (outerType != null)
                {
                    var outerTypeName = outerType.Name;

                    // 从类型名中提取原始方法名（如果可能）
                    // 例如：<>c__DisplayClass1_0 或 <>c__DisplayClass<Main>1_0
                    var methodMatch = Regex.Match(typeName, @"<([^>]+)>");
                    if (methodMatch.Success)
                    {
                        var originalMethod = methodMatch.Groups[1].Value;
                        return $"{outerTypeName}.{originalMethod}";
                    }

                    return outerTypeName;
                }

                // 如果找不到外部类型，尝试直接解析
                var directMatch = Regex.Match(methodName, @"<([^>]+)>");
                if (directMatch.Success)
                {
                    return directMatch.Groups[1].Value;
                }
            }
            catch
            {
                // 解析失败时忽略异常
            }

            return null;
        }

        private static void LogDirect(object message, string prefix = "Log", ConsoleColor color = ConsoleColor.Gray, bool save = true)
        {
            if (BHooks.HookHandlers.ReloadHandler.Caller is { RealPlayer: true })
            {
                var c = System.Drawing.Color.FromName(color.ToString());
                BHooks.HookHandlers.ReloadHandler.Caller.SendMessage(message.ToString(), new(c.R, c.G, c.B));
            }

            var callerInfo = GetMeaningfulCaller(0);
            var log = $"[{callerInfo}] <{prefix}> {message}";
            Console.ForegroundColor = color;
            Console.WriteLine(log);
            Console.ResetColor();
            if (save)
                TShock.Log?.Write(log, TraceLevel.Info);
        }
    }
}
