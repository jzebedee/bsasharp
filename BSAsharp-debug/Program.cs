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
            if (args.Length == 0)
            {
                Console.WriteLine("Use:");
                Console.WriteLine("bsasharp-debug <bsa path>");
                Console.ReadKey();
                return;
            }

            using (var fs = File.OpenRead(args[0]))
            {
                var bsaReader = new BSAsharp.BSAReader(fs);
                var layout = bsaReader.Read();

                foreach (var folder in layout)
                {
                    Console.WriteLine(folder.Path);
                    foreach (var child in folder.Children)
                    {
                        Console.Write('\t');
                        Console.WriteLine("{0} ({1} bytes, {2})", child.Name, child.Data.Length, child.IsCompressed ? "Compressed" : "Uncompressed");
                    }
                    Console.ReadKey();
                }
                Console.WriteLine();
                Console.WriteLine("Complete.");
                Console.ReadKey();
            }
        }
    }
}
