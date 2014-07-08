using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BSAsharp;

namespace BSAsharp_cli
{
    class Program
    {
        static void Main(string[] args)
        {
            if (args.Length == 0 || args.Length % 2 != 0)
            {
                Console.WriteLine("Use:");
                Console.WriteLine("bsasharp-cli <args>");
                Console.ReadKey();
                return;
            }

            string inPath = null, outFile = null, packFolder = null, unpackFolder = null;
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
                            inPath = a.val;
                            break;
                        case "OUT":
                            outFile = a.val;
                            break;
                        default:
                            Console.WriteLine("Command " + a.cmd + " unrecognized.");
                            break;
                    }
                });

            Trace.Assert(packFolder != null ^ inPath != null);

#if DEBUG
            int tests = 15;
            long[] ticks = new long[tests];
            for (int i = 0; i < tests; i++)
            {
#endif
                IEnumerable<BSAWrapper> BSAs = null;
                if (inPath != null)
                {
                    if (File.Exists(inPath))
                        BSAs = new[] { new BSAWrapper(inPath, new CompressionOptions(CompressionStrategy.Size | CompressionStrategy.Unsafe)) };
                    else if (Directory.Exists(inPath))
                        BSAs = Directory.EnumerateFiles(inPath, "*.bsa", SearchOption.TopDirectoryOnly).Select(bsapath => new BSAWrapper(bsapath));
                }
                else if (packFolder != null)
                    BSAs = new[] { new BSAWrapper(packFolder, new ArchiveSettings(true, false, new CompressionOptions(CompressionStrategy.Size))) };
                Trace.Assert(BSAs != null);

                foreach (var wrapper in BSAs)
                {
                    var watch = new Stopwatch();
                    try
                    {
                        watch.Start();

                        using (wrapper)
                        {
#if DEBUG
#else
                        foreach (var folder in wrapper)
                        {
                            Console.WriteLine(folder);
                            foreach (var file in folder)
                            {
                                Console.Write('\t');
                                Console.WriteLine("{0} ({1} bytes, {2})", file.Name, file.Size, file.IsCompressed ? "Compressed" : "Uncompressed");
                            }
                            //Console.ReadKey();
                        }
#endif

                            if (unpackFolder != null)
                                wrapper.Extract(unpackFolder);
                            if (outFile != null)
                                wrapper.Save(outFile);
                        }
                    }
                    finally
                    {
                        watch.Stop();
#if DEBUG
                        ticks[i] = watch.ElapsedTicks;
#endif
                    }

#if DEBUG
#else
                Console.WriteLine();
#endif
                    Console.WriteLine("Complete in " + watch.Elapsed.ToString());
                }
#if DEBUG
            }
#endif

            Console.WriteLine();
            Console.WriteLine("All operations complete");
#if DEBUG
            Console.WriteLine("Average time: " + TimeSpan.FromTicks((long)ticks.Average()));
#endif
            Console.ReadKey();
        }

#if DEBUG
        private static byte[] RandomBuffer(int size)
        {
            var buf = new byte[size];
            var rnd = new Random();
            for (int i = 0; i < buf.Length; i++)
                buf[i] = (byte)rnd.Next(byte.MaxValue);

            return buf;
        }
#endif
    }
}