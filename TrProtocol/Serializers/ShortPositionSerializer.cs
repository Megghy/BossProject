using System.IO;
using Terraria.DataStructures;

namespace TrProtocol.Models
{
    [Serializer(typeof(ShortPositionSerializer))]
    public partial struct ShortPosition
    {
        public static implicit operator Point16(ShortPosition p)
            => new(p.X, p.Y);
        public static implicit operator ShortPosition(Point16 p)
            => new(p.X, p.Y);
        public static implicit operator Point(ShortPosition p)
            => new(p.X, p.Y);
        public static implicit operator ShortPosition(Point p)
            => new(p.X, p.Y);
        private class ShortPositionSerializer : FieldSerializer<ShortPosition>
        {
            protected override ShortPosition _Read(BinaryReader br)
            {
                return new ShortPosition
                {
                    X = br.ReadInt16(),
                    Y = br.ReadInt16()
                };
            }

            protected override void _Write(BinaryWriter bw, ShortPosition t)
            {
                bw.Write(t.X);
                bw.Write(t.Y);
            }
        }
    }
}
