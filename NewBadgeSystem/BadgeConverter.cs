using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Terraria;

namespace BadgeSystem
{
    public sealed class BadgeConverter : JsonConverter
    {
        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            Content content = (Content)value;
            writer.Formatting = Formatting.Indented;
            writer.WriteStartObject();
            writer.WritePropertyName("Content");
            writer.WriteValue(content.ContentValue);
            writer.WritePropertyName("Identifier");
            writer.WriteValue(content.Identifier);
            writer.WritePropertyName("Color");
            writer.WriteValue(content.Color.Hex3());
            writer.WritePropertyName("Type");
            writer.WriteValue(content.Type);
            writer.WriteEndObject();

        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            JObject jObject = (JObject)serializer.Deserialize(reader);
            string content = jObject["Content"].Value<string>();
            string id = jObject["Identifier"].Value<string>();
            string colorHex = jObject["Color"].Value<string>();
            string type = jObject["Type"].Value<string>();
            return new Content(content, id, colorHex, type);
        }

        public override bool CanConvert(Type objectType)
        {
            return objectType == typeof(Content);
        }
    }
}
