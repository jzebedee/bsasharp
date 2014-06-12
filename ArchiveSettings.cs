using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BSAsharp
{
    public sealed class ArchiveSettings
    {
        public readonly bool DefaultCompressed, BStringPrefixed;

        public ArchiveSettings(bool DefaultCompressed, bool BStringPrefixed)
        {
            this.DefaultCompressed = DefaultCompressed;
            this.BStringPrefixed = BStringPrefixed;
        }
    }
}