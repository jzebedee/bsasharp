using System;
using System.Collections.Generic;

namespace BSAsharp
{
    public class CompressionOptions
    {
        public CompressionStrategy Strategy { get; set; }
        public Dictionary<string, int> ExtensionCompressionLevel { get; set; }

        public CompressionOptions(CompressionStrategy strategy = CompressionStrategy.Safe)
        {
            Strategy = strategy;
            ExtensionCompressionLevel = new Dictionary<string, int>();
        }

        public int SetCompressionLevel(string extension, int level)
        {
            if (string.IsNullOrEmpty(extension))
                throw new ArgumentNullException("extension");

            int oldLevel;
            if (!ExtensionCompressionLevel.TryGetValue(extension, out oldLevel))
            {
                ExtensionCompressionLevel.Add(extension, level);
            }
            else
            {
                ExtensionCompressionLevel[extension] = level;
            }

            return oldLevel;
        }

        public int? GetCompressionLevel(string extension)
        {
            if (string.IsNullOrEmpty(extension))
                throw new ArgumentNullException("extension");

            int level;
            if (ExtensionCompressionLevel.TryGetValue(extension, out level))
                return level;

            return null;
        }
    }
}
