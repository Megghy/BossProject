using BossFramework.BAttributes;
using Newtonsoft.Json;
using System.IO;

namespace BossFramework.BInterfaces
{
    public abstract class BaseConfig<T> where T : BaseConfig<T>, new()
    {
        protected static T _instance;
        protected virtual string FilePath => Path.Combine(TShockAPI.TShock.SavePath, $"{typeof(T).Name}.json");
        public static T Instance { get { _instance ??= Load(); return _instance; } }
        public static T Load()
        {
            T config = new();
            if (File.Exists(config.FilePath))
                config = JsonConvert.DeserializeObject<T>(File.ReadAllText(config.FilePath))!;
            config.Save();
            return config;
        }
        [Reloadable]
        public static void Reload()
        {
            _instance = null;
        }
        public virtual void Save()
        {
            if (!Directory.Exists(Directory.GetParent(FilePath).FullName))
                Directory.CreateDirectory(Directory.GetParent(FilePath).FullName);
            File.WriteAllText(FilePath, JsonConvert.SerializeObject(this, Formatting.Indented));
        }
    }
}
