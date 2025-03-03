using System.IO;
using System.Linq;
using BossFramework.BModels;
using Terraria;
using TShockAPI;

namespace BossFramework
{
    public static class BInfo
    {
        public static long GameTick => Main.GameUpdateCount;
        public const int ProjMaxLiveTick = 1000 * 60;
        public static string FilePath => Path.Combine(TShock.SavePath, "Boss");
        public static BPlayer[] OnlinePlayers
            => [.. TShock.Players.Where(p => p is { Active: true, RealPlayer: true }).Select(p => p.GetBPlayer())];
    }
}
