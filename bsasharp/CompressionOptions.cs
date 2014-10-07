using System;
using System.Collections.Generic;

namespace BSAsharp
{
    public class CompressionOptions
    {
        public CompressionStrategy Strategy { get; protected set; }
        public IDictionary<string, int> ExtensionCompressionLevel { get; protected set; }

        public CompressionOptions(CompressionStrategy strategy = CompressionStrategy.Safe)
        {
            Strategy = strategy;
            ExtensionCompressionLevel = new Dictionary<string, int>();
        }
    }
}
