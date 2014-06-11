using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BSAsharp;

namespace BSAsharp_debug
{
    class Program
    {
        static void Main(string[] args)
        {
            if (args.Length == 0 || args.Length % 2 != 0)
            {
                Console.WriteLine("Use:");
                Console.WriteLine("bsasharp-debug <args>");
                Console.ReadKey();
                return;
            }

            string inFile = null, outFile = null, packFolder = null, unpackFolder = null;
            args
                .Select((s, i) => s.Substring(0, 1) == "-" ? new { cmd = s.Substring(1), val = args[i + 1] } : null)
                .Where(a => a != null)
                .ToList()
                .ForEach(a =>
                {
                    switch (a.cmd.ToUpperInvariant())
                    {
                        case "PACK":
                            packFolder = a.val;
                            break;
                        case "UNPACK":
                            unpackFolder = a.val;
                            break;
                        case "IN":
                            inFile = a.val;
                            break;
                        case "OUT":
                            outFile = a.val;
                            break;
                        default:
                            Console.WriteLine("Command " + a.cmd + " unrecognized.");
                            break;
                    }
                });

            var extractWatch = new Stopwatch();
            try
            {
                extractWatch.Start();

                BSAWrapper wrapper = null;
                if (inFile != null)
                    wrapper = new BSAWrapper(inFile);
                else if (packFolder != null)
                    wrapper = new BSAWrapper(packFolder, new ArchiveSettings(true, false));
                Trace.Assert(wrapper != null);

                foreach (var folder in wrapper)
                {
                    Console.WriteLine(folder.Path);
                    foreach (var file in folder)
                    {
                        Console.Write('\t');
                        Console.WriteLine("{0} ({1} bytes, {2})", file.Name, file.Size, file.IsCompressed ? "Compressed" : "Uncompressed");
                    }
                    //Console.ReadKey();
                }

                if (unpackFolder != null)
                    wrapper.Extract(unpackFolder);
                if (outFile != null)
                    wrapper.Save(outFile);
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
