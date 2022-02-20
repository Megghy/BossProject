using System;
using System.IO;

namespace TrProtocol.Serializers
{
    public class ByteEnumSerializer<T> : FieldSerializer<T> where T : Enum
    {
        protected override T _Read(BinaryBufferReader br)
        {
            return (T)(object)br.ReadByte();
        }

        protected override void _Write(BinaryWriter bw, T t)
        {
            bw.Write((byte)(object)t);
        }
    }
    public class NormalEnumSerializer<T> : FieldSerializer<T> where T : Enum
    {
        protected override T _Read(BinaryBufferReader br)
        {
            return (T)(object)br.ReadByte();
        }

        protected override void _Write(BinaryWriter bw, T t)
        {
            bw.Write((byte)(object)t);
        }
    }
}
