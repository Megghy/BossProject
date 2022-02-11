using Newtonsoft.Json;
using System.IO;
using TShockAPI;

namespace PlotMarker
{
	[JsonObject(MemberSerialization.OptIn)]
	internal sealed class Configuration
	{
		public static readonly string ConfigPath = Path.Combine(TShock.SavePath, "PlotMarker.json");

		[JsonProperty("属地样式")]
		public Style PlotStyle =
			new Style
			{
				LineWidth = 3,
				CellWidth = 40,
				CellHeight = 30,
				TileId = 161,
				TilePaint = 21,
				WallId = 127,
				WallPaint = 21
			};

		[JsonProperty("默认玩家最大属地数")]
		public byte DefaultCellPerPlayer = 1;

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
}
