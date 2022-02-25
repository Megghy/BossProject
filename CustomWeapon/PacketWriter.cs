using System.IO;
using System.Text;

namespace CustomWeaponAPI;

public class PacketWriter
{
	private MemoryStream memoryStream;

	private BinaryWriter writer;

	public PacketWriter()
	{
		memoryStream = new MemoryStream();
		writer = new BinaryWriter(memoryStream);
		writer.BaseStream.Position = 3L;
	}

	public PacketWriter SetType(short type)
	{
		long position = writer.BaseStream.Position;
		writer.BaseStream.Position = 2L;
		writer.Write(type);
		writer.BaseStream.Position = position;
		return this;
	}

	public PacketWriter PackByte(byte num)
	{
		writer.Write(num);
		return this;
	}

	public PacketWriter PackInt16(short num)
	{
		writer.Write(num);
		return this;
	}

	public PacketWriter PackUInt16(ushort num)
	{
		writer.Write(num);
		return this;
	}

	public PacketWriter PackInt32(int num)
	{
		writer.Write(num);
		return this;
	}

	public PacketWriter PackUInt32(uint num)
	{
		writer.Write(num);
		return this;
	}

	public PacketWriter PackUInt64(ulong num)
	{
		writer.Write(num);
		return this;
	}

	public PacketWriter PackSingle(float num)
	{
		writer.Write(num);
		return this;
	}

	public PacketWriter PackString(string str)
	{
		writer.Write(str);
		return this;
	}

	private void UpdateLength()
	{
		long position = writer.BaseStream.Position;
		writer.BaseStream.Position = 0L;
		writer.Write((short)position);
		writer.BaseStream.Position = position;
	}

	public static string ByteArrayToString(byte[] ba)
	{
		StringBuilder stringBuilder = new StringBuilder(ba.Length * 2);
		for (int i = 0; i < ba.Length; i++)
		{
			stringBuilder.AppendFormat("{0:x2}", ba[i]);
		}
		return stringBuilder.ToString();
	}

	public byte[] GetByteData()
	{
		UpdateLength();
		return memoryStream.ToArray();
	}
}
