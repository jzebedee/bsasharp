using System;
using System.Collections.Generic;
using System.Diagnostics;
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

            //using (var wrapper = new BSAsharp.BSAWrapper(@"test.bsa"))
            using (var wrapper = new BSAsharp.BSAWrapper(args[0]))
            {
                wrapper.Save(args[1]);

                var extractWatch = new Stopwatch();
                try
                {
                    extractWatch.Start();

                    foreach (var folder in wrapper)
                    {
                        //Console.WriteLine(folder.Path);
                        foreach (var child in folder.Children)
                        {
                            //Console.Write('\t');
                            //Console.WriteLine("{0} ({1} bytes, {2})", child.Name, child.Data.Length, child.IsCompressed ? "Compressed" : "Uncompressed");
                        }
                        //Console.ReadKey();
                    }
                }
                finally
                {
                    extractWatch.Stop();
                }
                Console.WriteLine();
                Console.WriteLine("Complete in " + extractWatch.Elapsed.ToString() + ".");
                Console.ReadKey();
            }
        }
    }
}
