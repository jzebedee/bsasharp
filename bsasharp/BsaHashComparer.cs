using System;
using System.Collections.Generic;

namespace BSAsharp
{
    internal sealed class BsaHashComparer : IComparer<IBsaEntry>
    {
        //Comparisons are based ONLY on the hash, which is created only
        //from the filename in BSAFiles and the path in BSAFolders.
        //Don't try to use comparisons OUTSIDE of the folder!!
        private static readonly Lazy<BsaHashComparer> _instance = new Lazy<BsaHashComparer>(() => new BsaHashComparer());
        internal static BsaHashComparer Instance => _instance.Value;

        private BsaHashComparer()
        {
        }

        public int Compare(IBsaEntry x, IBsaEntry y)
        {
            return x.Hash.CompareTo(y.Hash);
        }
    }
}