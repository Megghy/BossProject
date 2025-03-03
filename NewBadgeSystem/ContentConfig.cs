using Newtonsoft.Json;
using TShockAPI;

namespace BadgeSystem
{
    public sealed class ContentConfig
    {
        public List<Content> Content = new List<Content>();

        public static readonly string FilePath = Path.Combine(TShock.SavePath, "newBadge.json");

        public bool TryParse(string str, out Content b, string type)
        {
            b = Content.SingleOrDefault((Content x) => string.Equals(x.Identifier, str, StringComparison.Ordinal));
            return b != null && b.Type == type;
        }

        public static ContentConfig Read()
        {
            if (!File.Exists(FilePath))
            {
                return new ContentConfig();
            }
            using FileStream stream = new FileStream(FilePath, FileMode.Open, FileAccess.Read, FileShare.Read);
            using StreamReader streamReader = new StreamReader(stream);
            return JsonConvert.DeserializeObject<ContentConfig>(streamReader.ReadToEnd());
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
