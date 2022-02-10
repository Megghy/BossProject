using Newtonsoft.Json;
using TShockAPI;

namespace BadgeSystem
{
    internal sealed class Configuration
    {
        public List<Badge> Badges = new List<Badge>();

        public static readonly string FilePath = Path.Combine(TShock.SavePath, "badge.json");

        public bool TryParse(string str, out Badge b)
        {
            b = Badges.SingleOrDefault((Badge x) => string.Equals(x.Identifier, str, StringComparison.Ordinal));
            return b != null;
        }

        public static Configuration Read()
        {
            if (!File.Exists(FilePath))
            {
                return new Configuration();
            }
            using FileStream stream = new FileStream(FilePath, FileMode.Open, FileAccess.Read, FileShare.Read);
            using StreamReader streamReader = new StreamReader(stream);
            return JsonConvert.DeserializeObject<Configuration>(streamReader.ReadToEnd());
        }

        public void Write()
        {
            using FileStream stream = new FileStream(FilePath, FileMode.Create, FileAccess.Write, FileShare.Write);
            string value = JsonConvert.SerializeObject(this, Formatting.Indented);
            using StreamWriter streamWriter = new StreamWriter(stream);
            streamWriter.Write(value);
        }
    }
}
