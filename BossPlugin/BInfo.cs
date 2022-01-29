using BossPlugin.BModels;
using System;
using System.IO;
using System.Linq;
using TShockAPI;

namespace BossPlugin
{
    public static class BInfo
    {
        public static string FilePath => Path.Combine(Environment.CurrentDirectory, TShock.SavePath, "Boss");

        public static BPlayer[] OnlinePlayers
            => TShock.Players.Where(p => p is { Active: true, RealPlayer: true })
            .Select(p => p.GetBPlayer())
            .ToArray();
    }
}
