using Microsoft.Xna.Framework;
using MultiSCore.Model;

namespace MultiSCore.Common
{
    /// <summary>
    /// 一个用于构建原始Terraria网络数据包的 Fluent Builder。
    /// </summary>
    public class RawDataBuilder
    {
        private readonly MemoryStream _memoryStream;
        private readonly BinaryWriter _writer;

        public RawDataBuilder(PacketTypes packetType)
        {
            _memoryStream = new MemoryStream();
            _writer = new BinaryWriter(_memoryStream);
            // 预留2字节的长度头
            _writer.BaseStream.Position = 2L;
            PackByte((byte)packetType);
        }

        public RawDataBuilder(int packetType) : this((PacketTypes)packetType) { }

        /// <summary>
        /// 创建一个自定义数据包。
        /// </summary>
        public RawDataBuilder(CustomPacketType packetType, string key)
        {
            _memoryStream = new MemoryStream();
            _writer = new BinaryWriter(_memoryStream);
            _writer.BaseStream.Position = 2L;
            PackByte((byte)15); // 使用一个不会被服务器特殊处理的包ID，如 ModPacket(27)
            PackByte((byte)packetType); // 自定义包类型
            PackString(key); // 安全密钥
        }

        public RawDataBuilder PackSByte(sbyte num)
        {
            _writer.Write(num);
            return this;
        }

        public RawDataBuilder PackByte(byte num)
        {
            _writer.Write(num);
            return this;
        }

        public RawDataBuilder PackInt16(short num)
        {
            _writer.Write(num);
            return this;
        }

        public RawDataBuilder PackUInt16(ushort num)
        {
            _writer.Write(num);
            return this;
        }

        public RawDataBuilder PackInt32(int num)
        {
            _writer.Write(num);
            return this;
        }

        public RawDataBuilder PackUInt32(uint num)
        {
            _writer.Write(num);
            return this;
        }

        public RawDataBuilder PackInt64(long num)
        {
            _writer.Write(num);
            return this;
        }

        public RawDataBuilder PackUInt64(ulong num)
        {
            _writer.Write(num);
            return this;
        }

        public RawDataBuilder PackSingle(float num)
        {
            _writer.Write(num);
            return this;
        }

        public RawDataBuilder PackString(string str)
        {
            _writer.Write(str ?? "");
            return this;
        }

        public RawDataBuilder PackRGB(Color color)
        {
            _writer.Write(color.R);
            _writer.Write(color.G);
            _writer.Write(color.B);
            return this;
        }
        public RawDataBuilder PackVector2(Vector2 v)
        {
            _writer.Write(v.X);
            _writer.Write(v.Y);
            return this;
        }

        private void UpdateLength()
        {
            long currentPosition = _writer.BaseStream.Position;
            _writer.BaseStream.Position = 0L;
            _writer.Write((short)currentPosition);
            _writer.BaseStream.Position = currentPosition;
        }

        public byte[] GetByteData()
        {
            UpdateLength();
            return _memoryStream.ToArray();
        }

        internal object PackInt16(int numberOfDeathsPVE)
        {
            throw new NotImplementedException();
        }
    }
}