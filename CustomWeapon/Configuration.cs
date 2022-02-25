// CustomWeaponPlugin.Configuration
using CustomWeaponAPI;
using Newtonsoft.Json;
using TShockAPI;

namespace CustomWeaponPlugin
{
    public class Configuration
    {
        public Dictionary<string, CustomWeapon> weapons;

        private static readonly string ConfigPath = Path.Combine(TShock.SavePath, "customweapon.json");

        public void Add(string name, CustomWeapon w)
        {
            if (weapons.ContainsKey(name))
            {
                weapons[name] = w;
            }
            else
            {
                weapons.Add(name, w);
            }
            Write();
        }

        public void Del(string name)
        {
            weapons.Remove(name);
            Write();
        }

        public static Configuration Read()
        {
            if (!File.Exists(ConfigPath))
            {
                return new Configuration
                {
                    weapons = new Dictionary<string, CustomWeapon>()
                };
            }
            using FileStream stream = new FileStream(ConfigPath, FileMode.Open, FileAccess.Read, FileShare.Read);
            using StreamReader streamReader = new StreamReader(stream);
            return JsonConvert.DeserializeObject<Configuration>(streamReader.ReadToEnd());
        }

        public void Write()
        {
            using FileStream stream = new FileStream(ConfigPath, FileMode.Create, FileAccess.Write, FileShare.Write);
            string value = JsonConvert.SerializeObject(this, Formatting.Indented);
            using StreamWriter streamWriter = new StreamWriter(stream);
            streamWriter.Write(value);
        }
    }
}
