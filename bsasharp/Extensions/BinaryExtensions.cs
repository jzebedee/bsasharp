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

        public static byte[] TrimBuffer(this byte[] buf, int offset, int length = -1)
        {
            length = length < 0 ? buf.Length - offset : length;
            if (offset == 0 && length == buf.Length)
                return buf;

            var newBuf = new byte[length];
            Buffer.BlockCopy(buf, offset, newBuf, 0, length);

            return newBuf;
        }

        public static string TrimStart(this string str, string toTrim)
        {
            if (str.Length >= toTrim.Length && str.IndexOf(toTrim, 0, toTrim.Length, StringComparison.Ordinal) == 0)
                return str.Substring(Math.Min(str.Length, toTrim.Length + 1));

            return str;
        }
    }
}
