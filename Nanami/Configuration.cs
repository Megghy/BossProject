using Microsoft.Xna.Framework;
using Newtonsoft.Json;
using TShockAPI;

namespace Nanami
{
    [JsonObject(MemberSerialization.OptIn)]
    public class Configuration
    {
        public static readonly string FilePath = Path.Combine(TShock.SavePath, "nanami.json");

        [JsonProperty("PvP玩家重生时间")]
        public int RespawnPvPSeconds = 5;

        [JsonProperty("连续击杀提示文本")]
        public KillText[] KillTexts =
        {
            new KillText
            {
                Text = "双杀!",
                R = 255,
                B = 0,
                G = 255
            },
            new KillText
            {
                Text = "连续消灭三人!",
                R = 255,
                B = 0,
                G = 0
            },
            new KillText
            {
                Text = "连续消灭四人! 吼啊!",
                R = 0,
                B = 255,
                G = 255
            },
            new KillText
            {
                Text = "成功取得五人斩!",
                R = 108,
                B = 166,
                G = 205
            },
            new KillText
            {
                Text = "连续歼灭六人! 来人阻止他!",
                R = 159,
                B = 182,
                G = 205
            },
            new KillText
            {
                Text = "连续杀了七个! 强啊",
                R = 219,
                B = 112,
                G = 147
            }
        };

        [JsonProperty("提示最少连续击杀")]
        public int MinKillTime = 2;

        [JsonProperty("自动播报最强玩家")]
        public bool AutoBroadcastBestKiller = true;

        [JsonProperty("自动播报时间间隔")]
        public int AutoBroadcastSeconds = 30;

        public static Configuration Read(string path)
        {
            if (!File.Exists(path))
                return new Configuration();
            using (var fs = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read))
            {
                using (var sr = new StreamReader(fs))
                {
                    var cf = JsonConvert.DeserializeObject<Configuration>(sr.ReadToEnd());
                    return cf;
                }
            }
        }

        public void Write(string path)
        {
            using (var fs = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.Write))
            {
                var str = JsonConvert.SerializeObject(this, Formatting.Indented);
                using (var sw = new StreamWriter(fs))
                {
                    sw.Write(str);
                }
            }
        }
    }

    public struct KillText
    {
        public string Text { get; set; }

        public byte R { get; set; }

        public byte B { get; set; }

        public byte G { get; set; }

        public string GetColorTag()
        {
            return TShock.Utils.ColorTag(Text, new Color(R, G, B));
        }
    }
}
