using System;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;

namespace BSAsharp.Extensions
{
    public static class BinaryExtensions
    {
        public static byte[] GetBytes<T>(T obj) where T : struct
        {
            byte[] buffer = new byte[Marshal.SizeOf(obj)];
            GCHandle? handle = null;
            try
            {
                handle = GCHandle.Alloc(buffer, GCHandleType.Pinned);
                Marshal.StructureToPtr(obj, handle.Value.AddrOfPinnedObject(), false);

                return buffer;
            }
            finally
            {
                handle?.Free();
            }
        }

        public static T Read<T>(this BinaryReader reader)
        {
            var buffer = new byte[Marshal.SizeOf<T>()];
            if (reader.Read(buffer, 0, buffer.Length) != buffer.Length)
                throw new InvalidOperationException("Bytes read did not match structure size");

            GCHandle? handle = null;
            try
            {
                handle = GCHandle.Alloc(buffer, GCHandleType.Pinned);
                return Marshal.PtrToStructure<T>(handle.Value.AddrOfPinnedObject());
            }
            finally
            {
                handle?.Free();
            }
        }

        public static T[] ReadArray<T>(this BinaryReader reader, int count)
        {
            var buffer = new byte[Marshal.SizeOf<T>() * count];
            if (reader.Read(buffer, 0, buffer.Length) != buffer.Length)
                throw new InvalidOperationException("Bytes read did not match expected size of structure array");

            var structArray = new T[count];

            GCHandle? handle = null;
            try
            {
                handle = GCHandle.Alloc(structArray, GCHandleType.Pinned);
                Marshal.Copy(buffer, 0, handle.Value.AddrOfPinnedObject(), buffer.Length);
                return structArray;
            }
            finally
            {
                handle?.Free();
            }
        }

        public static string ReadBString(this BinaryReader reader, bool stripEnd = false)
        {
            var length = reader.ReadByte();

            var bytes = reader.ReadBytes(length);
            var bstring = Encoding.Default.GetString(bytes);

            return stripEnd ? bstring.TrimEnd('\0') : bstring;
        }

        public static void WriteBString(this BinaryWriter writer, string toWrite)
        {
            var bytes = Encoding.Default.GetBytes(toWrite);

            writer.Write((byte)bytes.Length);
            writer.Write(bytes);
        }

        public static void WriteBZString(this BinaryWriter writer, string toWrite)
        {
            WriteBString(writer, toWrite.LastOrDefault() == '\0' ? toWrite : toWrite + "\0");
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
            var bytes = Encoding.Default.GetBytes(toWrite + "\0");
            writer.Write(bytes);
        }

        public static string TrimStart(this string str, string toTrim)
        {
            if (str.Length >= toTrim.Length && str.IndexOf(toTrim, 0, toTrim.Length, StringComparison.Ordinal) == 0)
                return str.Substring(Math.Min(str.Length, toTrim.Length + 1));

            return str;
        }
    }
}
