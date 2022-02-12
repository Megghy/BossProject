using System.IO;
using Terraria.Localization;

namespace TrProtocol.Models
{
    class NetworkSerializer : FieldSerializer<NetworkText>
    {
        protected override NetworkText _Read(BinaryReader br)
        {
            return NetworkText.Deserialize(br);
        }

        protected override void _Write(BinaryWriter bw, NetworkText t)
        {
            t.Serialize(bw);
        }
    }
}
