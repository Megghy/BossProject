﻿using BossFramework.BAttributes;
using Newtonsoft.Json;
using System.IO;

namespace BossFramework
{
    public class BConfig
    {
        private static BConfig _instance;
        public static BConfig Instance { get { _instance ??= Load(); return _instance; } }
        public static string ConfigPath => Path.Combine(BInfo.FilePath, "Config.json");
        public static BConfig Load()
        {
            BConfig config;
            if (File.Exists(ConfigPath))
                config = JsonConvert.DeserializeObject<BConfig>(File.ReadAllText(ConfigPath))!;
            else
                config = new BConfig();
            config.Save();
            return config;
        }
        public static void Reload()
        {
            _instance = null;
        }
        public void Save()
        {
            File.WriteAllText(ConfigPath, JsonConvert.SerializeObject(this, Formatting.Indented));
        }

        public bool FastLoadWorld { get; set; } = true;
        public bool DebugInfo { get; set; } = true;
    }
}