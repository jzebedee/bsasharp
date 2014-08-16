using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BSAsharp
{
    sealed class BSAHashComparer : IComparer<IBsaEntry>
    {
        //Comparisons are based ONLY on the hash, which is created only
        //from the filename in BSAFiles and the path in BSAFolders.
        //Don't try to use comparisons OUTSIDE of the folder!!
        public static readonly BSAHashComparer Instance = new BSAHashComparer();
        private BSAHashComparer()
        {
            
        }

        public int Compare(IBsaEntry x, IBsaEntry y)
        {
            return x.Hash.CompareTo(y.Hash);
        }
    }
}