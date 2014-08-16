using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BSAsharp
{
    public class CompressionOptions
    {
        public CompressionStrategy Strategy { get; set; }
        public Dictionary<string, int> ExtensionCompressionLevel { get; set; }

        public CompressionOptions(CompressionStrategy Strategy = CompressionStrategy.Safe)
        {
            this.Strategy = Strategy;
            ExtensionCompressionLevel = new Dictionary<string, int>();
        }

        public int SetCompressionLevel(string extension, int level)
        {
            if (string.IsNullOrEmpty(extension))
                throw new ArgumentNullException("extension");

            extension = extension.ToUpperInvariant();

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

            extension = extension.ToUpperInvariant();

            int level;
            if (!ExtensionCompressionLevel.TryGetValue(extension, out level))
                return null;
            return level;
        }
    }
}
