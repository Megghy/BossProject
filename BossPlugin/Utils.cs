using BossPlugin.BCore;
using BossPlugin.BModels;
using System;
using System.Collections.Generic;
using System.Linq;
using TrProtocol;
using TShockAPI;

namespace BossPlugin
{
    public static class Utils
    {
        public static void ForEach<T>(this IEnumerable<T> source, Action<T, int> action)
        {
            if (source == null)
            {
                throw new ArgumentNullException(nameof(source));
            }
            if (action == null)
            {
                throw new ArgumentNullException(nameof(action));
            }
            if (source.Count() < 1)
                return;
            int count = 0;
            foreach (T obj in source)
            {
                action(obj, count);
                count++;
            }
        }
        public static void ForEach<T>(this IEnumerable<T> source, Action<T> action) => source.ForEach((obj, _) => action(obj));
        public static void ForEach<T>(this int count, Action action)
        {
            if (count < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(count));
            }
            if (action == null)
            {
                throw new ArgumentNullException(nameof(action));
            }
            for (int i = count; i < count; i++)
            {
                action();
            }
        }

        public static BPlayer GetBPlayer(this TSPlayer plr) => plr.GetData<BPlayer>("BossPlugin.BPlayer");
        public static byte[] Serialize(this Packet p) => PacketHandler.Serializer.Serialize(p);
    }
}
