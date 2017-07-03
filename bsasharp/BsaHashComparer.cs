using System.Collections.Generic;

namespace BSAsharp
{
    sealed class BsaHashComparer : IComparer<IBsaEntry>
    {
        //Comparisons are based ONLY on the hash, which is created only
        //from the filename in BSAFiles and the path in BSAFolders.
        //Don't try to use comparisons OUTSIDE of the folder!!
        public static readonly BsaHashComparer Instance = new BsaHashComparer();
        private BsaHashComparer()
        {
            
        }

        public int Compare(IBsaEntry x, IBsaEntry y)
        {
            return x.Hash.CompareTo(y.Hash);
        }
    }
}