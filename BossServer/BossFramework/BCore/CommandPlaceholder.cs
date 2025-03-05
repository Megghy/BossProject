using System.Collections.Generic;
using System.Linq;
using BossFramework.BAttributes;
using BossFramework.BModels;
using BossFramework.DB;
using TShockAPI;

namespace BossFramework.BCore
{
    public static class CommandPlaceholder
    {
        public static List<PlaceholderInfo> Placeholders { get; private set; } = [];

        [AutoInit]
        private static void InitPlaceholder()
        {
            Placeholders = [.. DBTools.GetAll<PlaceholderInfo>()];
        }

        public static string ReplacePlaceholder(this string text, BPlayer plr)
        {
            foreach (var p in Placeholders)
            {
                if(p.Match(text))
                    text = p.Replace(new(plr), text);
            }
            return text;
        }
    }
}
