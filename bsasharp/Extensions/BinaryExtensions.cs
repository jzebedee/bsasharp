﻿using System;
using System.IO;
using System.IO.MemoryMappedFiles;
using System.Linq;
using System.Text;

namespace BSAsharp.Extensions
{
    public static class BinaryExtensions
    {
        public static readonly Encoding Windows1252 = Encoding.GetEncoding("Windows-1252");

        public static string ReadBString(this BinaryReader reader, bool stripEnd = false)
        {
            var length = reader.ReadByte();
            return ReadBString(reader, length, stripEnd);
        }

        public static string ReadBString(this BinaryReader reader, byte length, bool stripEnd = false)
        {
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
            var bytes = Windows1252.GetBytes(toWrite + "\0");
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

        internal static Stream ToStream(this MemoryMappedFile mmf, long offset, long size, MemoryMappedFileAccess rights)
        {
            return mmf.CreateViewStream(offset, size, rights);
        }

        internal static BinaryReader ToReader(this MemoryMappedFile mmf, long offset, long size)
        {
            return new BinaryReader(ToStream(mmf, offset, size, MemoryMappedFileAccess.Read));
        }
    }
}
