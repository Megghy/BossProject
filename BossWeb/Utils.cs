using System.Text.RegularExpressions;
using AntDesign;
using BossWeb.Core;
using Terraria;

namespace BossWeb
{
    public enum TerrariaContentTypes
    {
        ItemID,
        ColorText,
        Text
    }
    public interface ITerrariaContent
    {
        public TerrariaContentTypes Type { get; }
        public string Text { get; }
    }
    public class TerrariaColorTextContent(string text, string hex) : ITerrariaContent
    {
        public TerrariaContentTypes Type { get; } = TerrariaContentTypes.ColorText;
        public string Text { get; init; } = text;
        /// <summary>
        /// 颜色, 不包含#
        /// </summary>
        public string Hex { get; } = hex;
    }
    public class TerrariaItemIDContent(string text, int itemID) : ITerrariaContent
    {
        public TerrariaContentTypes Type { get; } = TerrariaContentTypes.ItemID;
        public string Text { get; } = text;
        public int ItemID { get; init; } = itemID;
        public string ItemName { get; set; }
    }
    public class TerrariaTextContent(string text) : ITerrariaContent
    {
        public TerrariaContentTypes Type { get; } = TerrariaContentTypes.Text;
        public string Text { get; } = text;
    }
    public static partial class Utils
    {
        public static void NotificateError(string message, string? description = null, double duration = 5) => Datas.NotificationQueue.TryAdd(new() { Message = message, Description = description, Duration = duration, NotificationType = NotificationType.Error });
        public static void NotificateSuccess(string message, string? description = null, double duration = 5) => Datas.NotificationQueue.TryAdd(new() { Message = message, Description = description, Duration = duration, NotificationType = NotificationType.Success });
        public static void NotificateInfo(string message, string? description = null, double duration = 5) => Datas.NotificationQueue.TryAdd(new() { Message = message, Description = description, Duration = duration, NotificationType = NotificationType.Info });

        public static List<ITerrariaContent> ParseContent(this string input)
        {
            // 正则表达式模式（启用忽略大小写）
            var pattern = TerrariaTextRegex();

            var matches = pattern.Matches(input);

            var results = new List<ITerrariaContent>();
            foreach (Match m in matches)
            {
                if (m.Groups["item"].Success)
                {
                    try
                    {
                        var itemId = int.Parse(m.Groups["itemId"].Value);
                        var name = Lang.GetItemNameValue(itemId);
                        results.Add(new TerrariaItemIDContent(m.Groups["item"].Value, itemId)
                        {
                            ItemName = name
                        });
                    }
                    catch
                    {
                        results.Add(new TerrariaTextContent(m.Groups["item"].Value));
                    }
                }
                else if (m.Groups["color"].Success)
                {
                    try
                    {
                        results.Add(new TerrariaColorTextContent(m.Groups["description"].Value, m.Groups["hexColor"].Value));
                    }
                    catch
                    {
                        results.Add(new TerrariaTextContent(m.Groups["color"].Value));
                    }
                }
                else if (m.Groups["text"].Success)
                {
                    results.Add(new TerrariaTextContent(m.Groups["text"].Value));
                }
            }

            return results;
        }

        [GeneratedRegex(@"(?<item>\[i:(?<itemId>\d+)\])|(?<color>\[c/(?<hexColor>[0-9A-Fa-f]{6}):(?<description>.*?)\])|(?<text>[^[]+)", RegexOptions.IgnoreCase | RegexOptions.IgnorePatternWhitespace, "zh-CN")]
        private static partial Regex TerrariaTextRegex();
    }
}
