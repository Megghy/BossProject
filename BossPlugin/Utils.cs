using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
    }
}
