using System;
using System.Diagnostics;
using System.IO;
using TableTweaker.Model;

namespace TableTweaker.Console
{
    class Program
    {
        static void Main(string[] args)
        {
            try
            {
                if (args.Length != 6)
                    throw new Exception("Usage: TableTweaker.Console <inputFile> <fieldDelimitter> <filter> <patternFile> <codeFile> <outputPath>");

                var input = File.ReadAllText(args[0]);
                var fieldDelimiter = string.IsNullOrEmpty(args[1]) ? ',' : args[1][0];
                var filter = string.IsNullOrEmpty(args[2]) ? ".*" : args[2];
                var pattern = string.IsNullOrEmpty(args[3]) ? "" : File.ReadAllText(args[3]);
                var code = string.IsNullOrEmpty(args[4]) ? "" : File.ReadAllText(args[4]);
                var outputPath = args[5];

                var engine = Engine.Instance;
                engine.FieldDelimiter = fieldDelimiter;
                var table = new Table(input, engine.FieldDelimiter, filter);

                var stopwatch = new Stopwatch();
                stopwatch.Reset();
                stopwatch.Start();
                var output = engine.Process(table, pattern, code).Replace("\r", "");
                stopwatch.Stop();

                File.WriteAllText(outputPath, output);

                var msg = $"{table.NumRows} filtered input rows processed in {stopwatch.ElapsedMilliseconds} ms";
                System.Console.WriteLine(msg);
            }
            catch (Exception ex)
            {
                System.Console.WriteLine("*** ERROR: " + ex);
            }

#if DEBUG
            System.Console.ReadLine();
#endif
        }
    }
}
