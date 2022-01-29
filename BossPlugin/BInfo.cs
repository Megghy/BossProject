using System;
using System.IO;
using TShockAPI;

namespace BossPlugin
{
    public static class BInfo
    {
        public static string FilePath => Path.Combine(Environment.CurrentDirectory, TShock.SavePath, "Boss");
    }
}
