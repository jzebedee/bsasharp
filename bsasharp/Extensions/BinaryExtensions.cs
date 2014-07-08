using System;
using System.Collections.Generic;
using System.IO;
using System.IO.MemoryMappedFiles;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;

namespace BSAsharp.Extensions
{
    public static class BinaryExtensions
    {
        public static readonly Encoding Windows1252 = Encoding.GetEncoding("Windows-1252");

        public static string ReadBString(this BinaryReader reader, bool stripEnd = false)
        {
            var length = reader.ReadByte();

            var bytes = reader.ReadBytes(length);
            var bstring = Windows1252.GetString(bytes);

            return stripEnd ? bstring.TrimEnd('\0') : bstring;
        }

        public static void WriteBString(this BinaryWriter writer, string toWrite)
        {
            var bytes = Windows1252.GetBytes(toWrite);

            writer.Write((byte)bytes.Length);
            writer.Write(bytes);
        }

        public static void WriteBZString(this BinaryWriter writer, string toWrite)
        {
            WriteBString(writer, toWrite.Last() == '\0' ? toWrite : toWrite + '\0');
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

        public static byte[] TrimBuffer(this byte[] buf, int offset, int length = -1)
        {
            if (offset == 0 && (length < 0 || length == buf.Length))
                return buf;

            var newLength = (length < 0 ? buf.Length - offset : length);
            var newBuf = new byte[newLength];
            Buffer.BlockCopy(buf, offset, newBuf, 0, newLength);

            return newBuf;
        }

        public static Stream ToStream(this MemoryMappedFile mmf, long offset, long size, MemoryMappedFileAccess rights = MemoryMappedFileAccess.ReadWrite)
        {
            return mmf.CreateViewStream(offset, size, rights);
        }

        public static Stream ToStream<T>(this MemoryMappedFile mmf, long offset, MemoryMappedFileAccess rights = MemoryMappedFileAccess.ReadWrite)
        {
            return ToStream(mmf, offset, Marshal.SizeOf(typeof(T)), rights);
        }

        public static Stream ToStreamBulk<T>(this MemoryMappedFile mmf, long offset, uint count, MemoryMappedFileAccess rights = MemoryMappedFileAccess.ReadWrite)
        {
            return ToStream(mmf, offset, Marshal.SizeOf(typeof(T)) * count, rights);
        }

        public static BinaryReader ToReader<T>(this MemoryMappedFile mmf, long offset)
        {
            return new BinaryReader(ToStream<T>(mmf, offset, MemoryMappedFileAccess.Read));
        }

        public static BinaryReader ToReaderBulk<T>(this MemoryMappedFile mmf, long offset, uint count)
        {
            return new BinaryReader(ToStreamBulk<T>(mmf, offset, count, MemoryMappedFileAccess.Read));
        }

        public static BinaryReader ToReader(this MemoryMappedFile mmf, long offset, long size)
        {
            return new BinaryReader(ToStream(mmf, offset, size, MemoryMappedFileAccess.Read));
        }

        public static BinaryWriter ToWriter<T>(this MemoryMappedFile mmf, long offset)
        {
            return new BinaryWriter(ToStream<T>(mmf, offset, MemoryMappedFileAccess.Write));
        }

        public static BinaryWriter ToWriter<T>(this MemoryMappedFile mmf, long offset, uint count)
        {
            return new BinaryWriter(ToStreamBulk<T>(mmf, offset, count, MemoryMappedFileAccess.Write));
        }

        public static BinaryWriter ToWriter(this MemoryMappedFile mmf, long offset, long size)
        {
            return new BinaryWriter(ToStream(mmf, offset, size, MemoryMappedFileAccess.Write));
        }
    }
}
