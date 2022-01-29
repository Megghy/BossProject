using Newtonsoft.Json;
using System.IO;

namespace BossPlugin
{
    public class BConfig
    {
        private static BConfig _instance;
        public static BConfig Instance { get { _instance ??= Load(); return _instance; } }
        public static string ConfigPath => Path.Combine(BInfo.FilePath, "Config.json");
        public static BConfig Load()
        {
            if (File.Exists(ConfigPath))
            {
                var config = JsonConvert.DeserializeObject<BConfig>(File.ReadAllText(ConfigPath));
                config.Save();
                return config;
            }
            else
            {
                var config = new BConfig();
                config.Save();
                return config;
            }
        }
        public static void Reload()
        {
            _instance = null;
        }
        public void Save()
        {
            File.WriteAllText(ConfigPath, JsonConvert.SerializeObject(this, Formatting.Indented));
        }
    }
}
