using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace FakeProvider
{
    /// <summary>
    /// Hey don't you love performance, then going back to unmanaged memory land is exactly what you need
    /// This class reads a byte array using pointers and unsafe code. 
    /// </summary>
    /// <remarks> 
    /// This reader assumes its on a Little Endian environment. It currently has no bound checks! 
    /// </remarks>
    public sealed unsafe class UnsafeBinaryReader : IDisposable
    {
        //TODO assert pointer positions/bound checks? Or maybe just yolo lolll
        //TODO: after its proven safe/reliable do a global terraria patcher that switches to use this
        private byte[] Data;

        public byte* dataPtr;

        private GCHandle handle;

        private bool disposedValue;

        public UnsafeBinaryReader(byte[] data)
        {
            Data = data;
            handle = GCHandle.Alloc(Data, GCHandleType.Pinned);
            fixed (byte* ptr = &(Data[0]))
            {
                dataPtr = ptr;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public short ReadInt16()
        {
            short value = *(short*)dataPtr;
            dataPtr += 2;
            return value;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int ReadInt32()
        {
            int value = *(int*)dataPtr;
            dataPtr += 4;
            return value;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public long ReadInt64()
        {
            long value = *(long*)dataPtr;
            dataPtr += 8;
            return value;
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ushort ReadUInt16()
        {
            ushort value = *(ushort*)dataPtr;
            dataPtr += 2;
            return value;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public uint ReadUInt32()
        {
            uint value = *(uint*)dataPtr;
            dataPtr += 4;
            return value;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ulong ReadUInt64()
        {
            ulong value = *(ulong*)dataPtr;
            dataPtr += 8;
            return value;
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public float ReadSingle()
        {
            float value = *(float*)dataPtr;
            dataPtr += 4;
            return value;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public double ReadDouble()
        {
            double value = *(double*)dataPtr;
            dataPtr += 8;
            return value;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Vector2 ReadVector2()
        {
            return new Vector2(ReadSingle(), ReadSingle());
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool ReadBoolean()
        {
            bool value = *(bool*)dataPtr;
            dataPtr += 1;
            return value;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public byte ReadByte()
        {
            byte value = *dataPtr;
            dataPtr += 1;
            return value;
        }

        //TODO: optimize, not much faster than BinaryReader.Readbytes(); 
        //Frankly have no idea how this can be done faster. 
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public byte[] ReadBytes(int count)
        {
            byte[] value = new byte[count];
            fixed (void* destinationPtr = value)
            {
                Unsafe.CopyBlock(destinationPtr, dataPtr, (uint)count);
            }
            dataPtr += count;
            return value;
        }

        //TODO: bound checks, verify it doesn't break on weird scenarios
        //TODO: optimize, not much faster than BinaryReader.ReadString(); check ReadBytes();
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public string ReadString()
        {
            int len = Read7BitEncodedInt();
            return Encoding.UTF8.GetString(ReadBytes(len));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private int Read7BitEncodedInt()
        {
            // Read out an Int32 7 bits at a time.  The high bit
            // of the byte when on means to continue reading more bytes.
            int count = 0;
            int shift = 0;
            byte b;
            do
            {
                // Check for a corrupted stream.  Read a max of 5 bytes.
                if (shift == 5 * 7)  // 5 bytes max per Int32, shift += 7
                    throw new FormatException("Format_Bad7BitInt32");

                // TODO: verify if this can cause read beyond bounds for unexpected behaviour/bound check strings better
                b = ReadByte();
                count |= (b & 0x7F) << shift;
                shift += 7;
            } while ((b & 0x80) != 0);
            return count;
        }

        public void Close()
        {
            Dispose(true);
        }

        private void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    GC.SuppressFinalize(this);
                }
                Data = null;
                handle.Free();
                disposedValue = true;
            }
        }

        ~UnsafeBinaryReader()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: false);
        }
        public void Dispose()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: true);
        }
    }
}
