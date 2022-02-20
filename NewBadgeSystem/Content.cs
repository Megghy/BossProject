using Microsoft.Xna.Framework;
using Newtonsoft.Json;
using System.Globalization;
using Terraria;
using TShockAPI;

namespace BadgeSystem
{
    [JsonConverter(typeof(BadgeConverter))]
    public sealed class Content
    {
        public string ContentValue { get; }

        public string Identifier { get; }

        public Color Color { get; }

        public string ColorHex { get; }

        public string Type { get; set; }

        public Content(string content, string id, string colorHex, string type)
        {
            ContentValue = content;
            Identifier = id;
            ColorHex = colorHex;
            Type = type;
            if (!TryParse(colorHex, out var color) && colorHex != "")
            {
                ColorHex = color.Hex3();
                TShock.Log.ConsoleError("无效数字代码");
            }
            Color = color;
        }

        public Content(string content, string id, Color color)
        {
            ContentValue = content;
            Identifier = id;
            Color = color;
        }

        public static bool TryParse(string value, out Color color)
        {
            color = default(Color);
            bool result;
            if (string.IsNullOrWhiteSpace(value))
            {
                result = false;
            }
            else if (value.Length != 6)
            {
                result = false;
            }
            else
            {
                byte[] array = new byte[3];
                for (int i = 0; i < value.Length; i += 2)
                {
                    string s = value[i].ToString() + value[i + 1];
                    if (!byte.TryParse(s, NumberStyles.HexNumber, null, out array[i / 2]))
                    {
                        return false;
                    }
                }
                color = new Color(array[0], array[1], array[2]);
                result = true;
            }
            return result;
        }
    }
}
