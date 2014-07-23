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
        public CompressionOptions Options { get; internal set; }

        public ArchiveSettings(bool DefaultCompressed, bool BStringPrefixed, CompressionOptions Options)
        {
            this.DefaultCompressed = DefaultCompressed;
            this.BStringPrefixed = BStringPrefixed;
            this.Options = Options;
        }
        internal ArchiveSettings()
        {
        }
    }
}
