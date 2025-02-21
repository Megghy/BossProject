using System.Collections.Concurrent;
using AntDesign;

namespace BossWeb.Core
{
    public static class Datas
    {
        public readonly static BlockingCollection<NotificationConfig> NotificationQueue = [];
        public static string[] Args = [];
    }
}
