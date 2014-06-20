﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BSAsharp
{
    public class CompressionOptions
    {
        public const CompressionStrategy DEFAULT_STRATEGY = CompressionStrategy.Size;

        public CompressionStrategy Strategy { get; set; }
        public Dictionary<string, int> ExtensionCompressionLevel { get; set; }

        public CompressionOptions(CompressionStrategy Strategy = DEFAULT_STRATEGY)
        {
            this.Strategy = Strategy;
            ExtensionCompressionLevel = new Dictionary<string, int>();
        }

        public int SetCompressionLevel(string extension, int level)
        {
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
            int level;
            if (!ExtensionCompressionLevel.TryGetValue(extension, out level))
                return null;
            return level;
        }
    }
}
