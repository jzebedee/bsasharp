using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BSAsharp
{
    class HashComparer : IComparer<IHashed>
    {
        public static readonly HashComparer Instance = new HashComparer();
        private HashComparer()
        {
            
        }

        public int Compare(IHashed x, IHashed y)
        {
            return x.Hash.CompareTo(y.Hash);
        }
    }
}