using System.IO;

namespace TrProtocol.Models
{
    public class Vector2Serailizer : FieldSerializer<Vector2>
    {
        protected override Vector2 _Read(BinaryBufferReader bb)
        {
            return new Vector2(bb.ReadSingle(), bb.ReadSingle());
        }

        protected override void _Write(BinaryWriter bb, Vector2 v)
        {
            bb.Write(v.X);
            bb.Write(v.Y);
        }
    }
}
