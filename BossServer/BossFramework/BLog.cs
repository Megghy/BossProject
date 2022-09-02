using System.Diagnostics;
using System.Linq;
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
            var caller = new StackFrame(1).GetMethod();
            var debugText = $"[{caller!.DeclaringType!.Name}.{caller.Name}] <DEBUG> {text}";
            Console.ForegroundColor = ConsoleColor.DarkCyan;
            Console.WriteLine(debugText);
            Console.ResetColor();
            BInfo.OnlinePlayers.Where(p => p.TsPlayer?.HasPermission("boss.admin.debug") == true)
                .ForEach(p => BUtils.SendMsg(p.TsPlayer, debugText, new(130, 200, 200)));
        }
        private static void LogDirect(object message, string prefix = "Log", ConsoleColor color = ConsoleColor.Gray, bool save = true)
        {
            if (BHooks.HookHandlers.ReloadHandler.Caller is { RealPlayer: true })
            {
                var c = System.Drawing.Color.FromName(color.ToString());
                BHooks.HookHandlers.ReloadHandler.Caller.SendMessage(message.ToString(), new(c.R, c.G, c.B));
            }
            var caller = new StackFrame(2).GetMethod();
            var from = $"{caller!.DeclaringType!.Name}.{caller.Name}";
            var log = $"[{from}] <{prefix}> {message}";
            Console.ForegroundColor = color;
            Console.WriteLine(log);
            Console.ResetColor();
            if (save)
                TShock.Log.Write(log, TraceLevel.Info);
        }
    }
}
