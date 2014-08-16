using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BSAsharp
{
    interface IBsaEntry
    {
        ulong Hash { get; }
        string Path { get; }
    }
}
