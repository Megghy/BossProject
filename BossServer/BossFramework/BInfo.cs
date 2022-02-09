using BossFramework.BModels;
using System;
using System.IO;
using System.Linq;
using TShockAPI;

namespace BossFramework
{
    public static class BInfo
    {
        public static long GameTick { get; internal set; } = 0;
        public static string FilePath => Path.Combine(Environment.CurrentDirectory, "Boss");
        public static BPlayer[] OnlinePlayers
            => TShock.Players.Where(p => p is { Active: true, RealPlayer: true })
            .Select(p => p.GetBPlayer())
            .ToArray();
    }
}
