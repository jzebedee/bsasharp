using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BSAsharp
{
    public sealed class ArchiveSettings
    {
        public bool DefaultCompressed { get; internal set; }
        public bool BStringPrefixed { get; internal set; }
        public CompressionStrategy Strategy { get; internal set; }

        public ArchiveSettings(bool DefaultCompressed, bool BStringPrefixed, CompressionStrategy Strategy)
        {
            this.DefaultCompressed = DefaultCompressed;
            this.BStringPrefixed = BStringPrefixed;
            this.Strategy = Strategy;
        }
        internal ArchiveSettings()
        {
        }
    }
}