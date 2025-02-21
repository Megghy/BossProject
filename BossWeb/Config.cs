using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Unicode;

namespace BossWeb
{
    public class Config
    {
        public static readonly JsonSerializerOptions DefaultSerializerOptions = new()
        {
            WriteIndented = true,
            Encoder = JavaScriptEncoder.Create(UnicodeRanges.All),
        };
        private static Config _oldInstance = new();
        private static Config? _instance;
        public static Config Instance { get { _instance ??= Load(); return _instance; } }
        public static string ConfigPath => Path.Combine(AppContext.BaseDirectory, "Config.json");
        static bool _first = true;
        public static Config Load()
        {
            if (File.Exists(ConfigPath))
            {
                try
                {
                    var config = Newtonsoft.Json.JsonConvert.DeserializeObject<Config>(File.ReadAllText(ConfigPath))!;
                    _oldInstance = config;
                    if (_first)
                        config.Save();
                    return _oldInstance;
                }
                catch
                {
                    Console.WriteLine($"配置文件读取失败");
                    return _oldInstance!;
                }
            }
            else
            {
                var config = new Config();
                config.Save();
                return config;
            }
        }
        public static void Reload()
        {
            _oldInstance = _instance;
            _instance = null;
        }
        public void Save()
        {
            File.WriteAllText(ConfigPath, JsonSerializer.Serialize(this, DefaultSerializerOptions));
        }

        public string StartupCommandLine { get; set; } = "-language zh-Hans -ip 0.0.0.0 -port 7777 -maxplayers 255 -world D:\\Code\\BossPlugin\\Output\\BOSS.wld";
    }
}
