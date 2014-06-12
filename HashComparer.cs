﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BSAsharp
{
    class HashComparer : IComparer<IHashed>
    {
        //Comparisons are based ONLY on the hash, which is created only
        //from the filename in BSAFiles and the path in BSAFolders.
        //Don't try to use SortedSet<T> features OUTSIDE of the folder!!
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