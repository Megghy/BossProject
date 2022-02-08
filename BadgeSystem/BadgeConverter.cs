using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Terraria;

namespace BadgeSystem
{
	public sealed class BadgeConverter : JsonConverter
	{
		public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
		{
			Badge badge = (Badge)value;
			writer.Formatting = Formatting.Indented;
			writer.WriteStartObject();
			writer.WritePropertyName("Content");
			writer.WriteValue(badge.Content);
			writer.WritePropertyName("Identifier");
			writer.WriteValue(badge.Identifier);
			writer.WritePropertyName("Color");
			writer.WriteValue(badge.Color.Hex3());
			writer.WriteEndObject();
		}

		public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
		{
			JObject jObject = (JObject)serializer.Deserialize(reader);
			string content = jObject["Content"].Value<string>();
			string id = jObject["Identifier"].Value<string>();
			string colorHex = jObject["Color"].Value<string>();
			return new Badge(content, id, colorHex);
		}

		public override bool CanConvert(Type objectType)
		{
			return objectType == typeof(Badge);
		}
	}
}
