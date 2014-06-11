using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BSAsharp
{
    class BSAFolderComparer : IComparer<BSAFolder>
    {
        public int Compare(BSAFolder x, BSAFolder y)
        {
            return x.Hash.CompareTo(y.Hash);
        }
    }
}