using System.IO;

namespace TrProtocol.Models
{
    class NetworkSerializer : FieldSerializer<NetworkText>
    {
        protected override NetworkText _Read(BinaryBufferReader br)
        {
            return NetworkText.Deserialize(br);
        }

        protected override void _Write(BinaryWriter bw, NetworkText t)
        {
            t.Serialize(bw);
        }
    }
}
