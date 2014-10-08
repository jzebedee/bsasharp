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
            try
            {
                return Dynamic.InvokeGetChain(model, aspect);
                return aspect.Any(c => c == '.')
                    ? Dynamic.InvokeGetChain(model, aspect)
                    : Dynamic.InvokeGet(model, aspect);
            }
            catch (RuntimeBinderException)
            {
                //IgnoredTypes.Add(model.GetType(), aspect);
            }

            return null;
        }

        internal static dynamic Set(object model, string aspect, object value)
        {
            return Dynamic.InvokeSetChain(model, aspect, value);
        }
    }
}
