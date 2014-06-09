using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BSAsharp_debug
{
    class Program
    {
        static void Main(string[] args)
        {
            using (var fs = File.OpenRead(@"X:\Games\Fallout3\Data\Fallout - Misc.bsa"))
            {
                var x = new BSAsharp.BSAReader(fs);
                x.Read();
            }
        }
    }
}
