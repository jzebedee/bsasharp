using System;

namespace BSAsharp.Format
{
    [Flags]
    public enum ArchiveFlags
    {
        NamedDirectories = 1 << 0,
        NamedFiles = 1 << 1,
        DefaultCompressed = 1 << 2,
        BStringPrefixed = 1 << 8,
    }
}