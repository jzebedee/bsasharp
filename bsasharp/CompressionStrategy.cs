using System;

namespace BSAsharp
{
    [Flags]
    public enum CompressionStrategy
    {
        /// <summary>
        /// Original compression state is preserved
        /// </summary>
        Safe = 0,
        /// <summary>
        /// Uncompressed files may be compressed
        /// </summary>
        Unsafe = 1 << 0,
        /// <summary>
        /// Compression strategy favors fast compression
        /// </summary>
        Speed = 1 << 1,
        /// <summary>
        /// Compression strategy favors high compression
        /// </summary>
        Size = 1 << 2,
        /// <summary>
        /// Compression strategy that tries many different strategies until a good ratio (or no compression) is found. Very slow!
        /// </summary>
        Aggressive = 1 << 3,
    }
}
