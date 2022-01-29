using System;
using System.Diagnostics;
using TShockAPI;

namespace BossPlugin
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
        private static void LogDirect(object message, string prefix = "Log", ConsoleColor color = ConsoleColor.Gray, bool save = true)
        {
            var caller = new StackFrame(2).GetMethod();
            var log = $"[BossPlugin] <{prefix}> {(prefix == "Error" ? $"- {caller.DeclaringType.Namespace}.{caller.Name} - " : "")}{message}";
            Console.ForegroundColor = color;
            Console.WriteLine(log);
            Console.ResetColor();
            if (save)
                TShock.Log.Write(log, TraceLevel.Info);
        }
    }
}
