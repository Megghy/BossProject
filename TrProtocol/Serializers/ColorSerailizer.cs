using System.IO;

namespace TrProtocol.Models
{
    public class ColorSerailizer : FieldSerializer<Color>
    {
        protected override Color _Read(BinaryBufferReader br)
        {
            return new Color(br.ReadByte(), br.ReadByte(), br.ReadByte());
        }

        protected override void _Write(BinaryWriter bw, Color c)
        {
            bw.Write(c.R);
            bw.Write(c.G);
            bw.Write(c.B);
        }
    }
}
