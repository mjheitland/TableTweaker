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
                if (args.Length != 7)
                    throw new Exception("Usage: TableTweaker.Console <inputFile> <fieldDelimitter> (quotedFields | unquotedFields) <filter> <patternFile> <codeFile> <outputPath>");

                var input = File.ReadAllText(args[0]);
                var fieldDelimiter = string.IsNullOrEmpty(args[1]) ? ',' : args[1][0];
                var quotedFields = args[2] == "quotedFields";
                var filter = string.IsNullOrEmpty(args[3]) ? ".*" : args[2];
                var pattern = string.IsNullOrEmpty(args[4]) ? "" : File.ReadAllText(args[3]);
                var code = string.IsNullOrEmpty(args[5]) ? "" : File.ReadAllText(args[4]);
                var outputPath = args[6];

                var engine = Engine.Instance;
                engine.FieldDelimiter = fieldDelimiter;
                engine.QuotedFields = quotedFields;
                var table = new Table(input, engine.FieldDelimiter, engine.QuotedFields, filter);

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
