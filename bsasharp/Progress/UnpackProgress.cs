using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BSAsharp.Progress
{
    public struct UnpackProgress
    {
        readonly string _filename;
        readonly int _count;
        readonly int _total;

        public string Filename { get { return _filename; } }
        public int Count { get { return _count; } }
        public int Total { get { return _total; } }

        public UnpackProgress(string filename, int count, int total)
        {
            _filename = filename;
            _count = count;
            _total = total;
        }
    }
}
