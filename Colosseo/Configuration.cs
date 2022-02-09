using Newtonsoft.Json;
using Terraria.ID;
using TShockAPI;
using TShockAPI.DB;

namespace Colosseo
{
    [JsonObject(MemberSerialization.OptIn)]
    internal class Configuration
    {
        public sealed class Ids
        {
            private readonly bool[] _array;

            public Ids(int[] list, int size)
            {
                _array = new SetFactory(size).CreateBoolSet(list);
            }

            public bool Valid(int value)
            {
                return _array[value];
            }
        }

        public static readonly string FilePath = Path.Combine(TShock.SavePath, "Colosseo.json");

        [JsonProperty("战场区域")]
        public string Venue { get; set; }

        [JsonProperty("净空区域")]
        public string Clear { get; set; }

        [JsonProperty("默认冷却秒")]
        public int DefaultCd { get; set; } = 10;


        [JsonProperty("默认召唤数量")]
        public int DefaultMaxSpawnAmount { get; set; } = 2;


        [JsonProperty("区域内最大怪物数量")]
        public int MaxSpawnAmountInArea { get; set; } = 100;


        [JsonProperty("禁止生成怪物")]
        public int[] Harmfuls { get; set; } = new int[18]
        {
            69, 70, 72, 398, 400, 397, 401, 396, 422, 507,
            493, 517, 59, 113, 114, 68, 439, 440
        };


        public Region VenueRegion { get; private set; }

        public Region ClearRegion { get; private set; }

        public bool InitSuccess { get; private set; }

        public Ids HarmfulNpcs { get; private set; }

        public void LoadRegions()
        {
            try
            {
                VenueRegion = TShock.Regions.GetRegionByName(Venue);
                ClearRegion = TShock.Regions.GetRegionByName(Clear);
                if (VenueRegion == null || ClearRegion == null)
                {
                    TShock.Log.ConsoleError("区域加载失败,请确认数据库和配置文件的区域匹配");
                    InitSuccess = false;
                }
                else
                {
                    HarmfulNpcs = new Ids(Harmfuls, 580);
                    InitSuccess = true;
                }
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }

        }

        public void Write(string path)
        {
            using FileStream stream = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.Write);
            string value = JsonConvert.SerializeObject(this, Formatting.Indented);
            using StreamWriter streamWriter = new StreamWriter(stream);
            streamWriter.Write(value);
        }

        public static Configuration Read(string path)
        {
            if (!File.Exists(path))
            {
                return new Configuration();
            }
            Configuration result;
            using (FileStream stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read))
            {
                using StreamReader streamReader = new StreamReader(stream);
                result = JsonConvert.DeserializeObject<Configuration>(streamReader.ReadToEnd());
            }
            return result;
        }
    }
}
