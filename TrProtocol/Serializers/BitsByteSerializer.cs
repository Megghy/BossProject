using System.IO;

namespace TrProtocol.Models
{
    [Serializer(typeof(BitsByteSerializer))]
    public partial struct ProtocolBitsByte
    {
        private class BitsByteSerializer : FieldSerializer<ProtocolBitsByte>
        {
            protected override ProtocolBitsByte _Read(BinaryBufferReader br)
            {
                return br.ReadByte();
            }

            protected override void _Write(BinaryWriter bw, ProtocolBitsByte t)
            {
                bw.Write(t);
            }
        }
    }
}
