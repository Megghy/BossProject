using AntDesign;
using BossWeb.Core;

namespace BossWeb
{
    public static class Utils
    {
        public static void NotificateError(string message, string? description = null, double duration = 5) => Datas.NotificationQueue.TryAdd(new() { Message = message, Description = description, Duration = duration, NotificationType = NotificationType.Error });
        public static void NotificateSuccess(string message, string? description = null, double duration = 5) => Datas.NotificationQueue.TryAdd(new() { Message = message, Description = description, Duration = duration, NotificationType = NotificationType.Success });
        public static void NotificateInfo(string message, string? description = null, double duration = 5) => Datas.NotificationQueue.TryAdd(new() { Message = message, Description = description, Duration = duration, NotificationType = NotificationType.Info });
    }
}
