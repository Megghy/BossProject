using System;
using System.Buffers.Binary;
using System.IO;

namespace TrProtocol
{
    public class BinaryBufferReader : BinaryReader
    {
        private byte[] _data;
        private readonly int startIndex;
        private long _position
        {
            get
                => BaseStream.Position + startIndex;
            set
                => BaseStream.Position = value - startIndex;
        }
        private int _length
            => (int)BaseStream.Length;

        public BinaryBufferReader(byte[] data) : base(new MemoryStream(data))
        {
            _data = data ?? throw new ArgumentNullException(nameof(data));
            _position = 0;
            startIndex = 0;
        }

        public BinaryBufferReader(byte[] data, int position, int length) : base(new MemoryStream(data, position, length))
        {
            _data = data ?? throw new ArgumentNullException(nameof(data));
            startIndex = position;
            _position = position;
        }

        public BinaryBufferReader(in ArraySegment<byte> data) : base(new MemoryStream(data.Array))
        {

            _data = data.Array ?? throw new ArgumentNullException(nameof(data));
            Position = data.Offset;
            _position = data.Offset;
        }

        public long Position
        {
            get => BaseStream.Position;
            set
            {
                if (value > _length)
                    throw new ArgumentOutOfRangeException(nameof(value), "The new position cannot be larger than the length");
                if (value < 0)
                    throw new ArgumentOutOfRangeException(nameof(value), "The new position is invalid");
                BaseStream.Position = value;
            }
        }
        public byte ReadInt8()
            => InternalReadByte();
        public override short ReadInt16() => BinaryPrimitives.ReadInt16LittleEndian(InternalReadSpan(2));

        public override ushort ReadUInt16() => BinaryPrimitives.ReadUInt16LittleEndian(InternalReadSpan(2));

        public override int ReadInt32() => BinaryPrimitives.ReadInt32LittleEndian(InternalReadSpan(4));

        public override uint ReadUInt32() => BinaryPrimitives.ReadUInt32LittleEndian(InternalReadSpan(4));

        public override long ReadInt64() => BinaryPrimitives.ReadInt64LittleEndian(InternalReadSpan(8));

        public override ulong ReadUInt64() => BinaryPrimitives.ReadUInt64LittleEndian(InternalReadSpan(8));

#if NETFRAMEWORK || NETSTANDARD2_0
		public virtual unsafe float ReadSingle()
		{
			var m_buffer = InternalReadSpan(4);
			uint tmpBuffer = (uint)(m_buffer[0] | m_buffer[1] << 8 | m_buffer[2] << 16 | m_buffer[3] << 24);
			return *((float*)&tmpBuffer);
		}
#else
        public override float ReadSingle() =>
            BitConverter.Int32BitsToSingle(BinaryPrimitives.ReadInt32LittleEndian(InternalReadSpan(4)));
#endif

        public override double ReadDouble() =>
            BitConverter.Int64BitsToDouble(BinaryPrimitives.ReadInt64LittleEndian(InternalReadSpan(8)));

        public override decimal ReadDecimal()
        {
            var buffer = InternalReadSpan(16);
            try
            {
                return new decimal(
                    new[]
                    {
                        BinaryPrimitives.ReadInt32LittleEndian(buffer), // lo
						BinaryPrimitives.ReadInt32LittleEndian(buffer[4..]), // mid
						BinaryPrimitives.ReadInt32LittleEndian(buffer[8..]), // hi
						BinaryPrimitives.ReadInt32LittleEndian(buffer[12..]) // flags
					});
            }
            catch (ArgumentException e)
            {
                // ReadDecimal cannot leak out ArgumentException
                throw new IOException("Failed to read decimal value", e);
            }
        }

        public override byte ReadByte() => InternalReadByte();

        public override byte[] ReadBytes(int count) => InternalReadSpan(count).ToArray();

        public virtual ReadOnlySpan<byte> ReadSpan(int count) => InternalReadSpan(count);

        public override sbyte ReadSByte() => (sbyte)InternalReadByte();

        public override bool ReadBoolean() => InternalReadByte() != 0;
        public override string ReadString()
            => base.ReadString();

        protected byte InternalReadByte()
        {
            if (Position + 1 > _length)
                throw new EndOfStreamException($"Reached to end of data [Length: {_length}, Target: {_position + 1}]");
            var originPos = _position;
            _position += 1;
            return _data[originPos];
        }

        protected ReadOnlySpan<byte> InternalReadSpan(int count)
        {
            if (Position + count > _length)
                throw new EndOfStreamException($"Reached to end of data [Length: {_length}, InnerPosition: {_position}, Target: {_position + count}]");

            var originPos = _position;
            _position += count;
            return new ReadOnlySpan<byte>(_data, (int)originPos, count);
        }
        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            BaseStream?.Dispose();
            _data = null;
        }
    }
}
