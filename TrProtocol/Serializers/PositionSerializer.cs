using System.IO;

namespace TrProtocol.Models
{
    class PositionSerializer : FieldSerializer<Point>
    {
        protected override Point _Read(BinaryBufferReader br)
        {
            return new Point
            {
                X = br.ReadInt32(),
                Y = br.ReadInt32()
            };
        }

        protected override void _Write(BinaryWriter bw, Point t)
        {
            bw.Write(t.X);
            bw.Write(t.Y);
        }
    }
}
