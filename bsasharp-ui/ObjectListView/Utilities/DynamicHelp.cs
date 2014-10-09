using System;
using System.Collections.Generic;
using System.Linq;
using Dynamitey;
using Microsoft.CSharp.RuntimeBinder;

namespace BrightIdeasSoftware.Utilities
{
    internal static class DynamicHelp
    {
        internal static dynamic Get(object model, string aspect)
        {
            return Dynamic.InvokeGetChain(model, aspect);
        }

        internal static object Set(object model, string aspect, object value)
        {
            return Dynamic.InvokeSetChain(model, aspect, value);
        }
    }
}
