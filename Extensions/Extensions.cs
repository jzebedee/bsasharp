using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;

namespace BSAsharp.Extensions
{
    public static class Extensions
    {
        public static readonly Encoding Windows1252 = Encoding.GetEncoding("Windows-1252");

        public static T ReadStruct<T>(this BinaryReader reader, int? Size = null)// where T : struct
        {
            int structSize = Size ?? Marshal.SizeOf(typeof(T));
            byte[] readBytes = reader.ReadBytes(structSize);
            if (readBytes.Length != structSize)
                throw new ArgumentException("Size of bytes read did not match struct size");

            return readBytes.MarshalStruct<T>(length: structSize);
        }

        public static IEnumerable<T> ReadBulkStruct<T>(this BinaryReader reader, int Count, int? Size = null)// where T : struct
        {
            int structSize = Size ?? Marshal.SizeOf(typeof(T));
            int bulkLength = structSize * Count;

            byte[] readBytes = reader.ReadBytes(bulkLength);
            if (readBytes.Length != bulkLength)
                throw new ArgumentException("Size of bytes read did not match expected size");

            for (int i = 0; i < Count; i++)
                yield return readBytes.MarshalStruct<T>(i * structSize, structSize);
        }

        public static string ReadBString(this BinaryReader reader, bool stripEnd = false)
        {
            var length = reader.ReadByte();

            var bytes = reader.ReadBytes(length);
            var bstring = Windows1252.GetString(bytes);

            return stripEnd ? bstring.TrimEnd('\0') : bstring;
        }

        public static void WriteBString(this BinaryWriter writer, string toWrite)
        {
            var bytes = Windows1252.GetBytes(toWrite + '\0');

            writer.Write((byte)bytes.Length);
            writer.Write(bytes);
        }

        public static string ReadCString(this BinaryReader reader)
        {
            var builder = new StringBuilder();

            byte cur;
            while ((cur = reader.ReadByte()) != '\0') builder.Append((char)cur);

            return builder.ToString();
        }

        public static void WriteCString(this BinaryWriter writer, string toWrite)
        {
            var bytes = Windows1252.GetBytes(toWrite + '\0');
            writer.Write(bytes);
        }

        //public static async Task<T> ReadStructAsync<T>(this BinaryReader reader, int? Size = null) where T : struct
        //{
        //    int structSize = Size ?? Marshal.SizeOf(typeof(T));
        //    byte[] readBytes = await reader.ReadBytesAsync(structSize);
        //    if (readBytes.Length != structSize)
        //        throw new ArgumentException("Size of bytes read did not match struct size");

        //    return readBytes.MarshalStruct<T>();
        //}

        public static T MarshalStruct<T>(this byte[] buf, int offset = 0, int length = -1)// where T : struct
        {
            buf = TrimBuffer(buf, offset, length);

            GCHandle pin_bytes = GCHandle.Alloc(buf, GCHandleType.Pinned);
            try
            {
                return (T)Marshal.PtrToStructure(pin_bytes.AddrOfPinnedObject(), typeof(T));
            }
            finally
            {
                pin_bytes.Free();
            }
        }

        public static byte[] TrimBuffer(this byte[] buf, int offset, int length = -1)
        {
            if (offset == 0 && length < 0)
                return buf;

            var newLength = (length < 0 ? buf.Length - offset : length);
            var newBuf = new byte[newLength];
            Buffer.BlockCopy(buf, offset, newBuf, 0, length);

            return newBuf;
        }

        public static void WriteStruct<T>(this BinaryWriter writer, T obj)// where T : struct
        {
            int size = Marshal.SizeOf(obj);
            var buf = new byte[size];

            IntPtr ptrAlloc = IntPtr.Zero;

            try
            {
                ptrAlloc = Marshal.AllocHGlobal(size);

                Marshal.StructureToPtr(obj, ptrAlloc, true);
                Marshal.Copy(ptrAlloc, buf, 0, size);
            }
            finally
            {
                if (ptrAlloc != IntPtr.Zero)
                    Marshal.FreeHGlobal(ptrAlloc);
            }

            writer.Write(buf);
        }
    }
}