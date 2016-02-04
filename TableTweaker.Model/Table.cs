using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text.RegularExpressions;

namespace TableTweaker.Model
{
    public class Table
    {
        // data
        public List<string> Rows { get; }
        public List<List<string>> RowFields { get; }

        // derived properties
        public string Header => Rows[0];
        public List<string> HeaderFields => RowFields[0];
        public int NumFields => HeaderFields.Count;
        public int NumRows => Rows.Count;

        public Table(string input, char fieldDelimiter, string filter)
        {
            Rows = new List<string>();
            RowFields = new List<List<string>>();
            
            var csvStringConverter = new CsvStringConverter(fieldDelimiter);

            var sentinelIndex = input.Length;
            var numFields = 0;
            var startIndex0 = 0;
            int startIndex1;
            var lineNo = 1;

            var regexFilter = new Regex(filter);

            // add data row by row to table
            do
            {
                var fields = new List<string>();

                startIndex1 = csvStringConverter.ConvertCsv(input, startIndex0, sentinelIndex, fields);

                var row = input.Substring(startIndex0, startIndex1 - startIndex0);
                if (row.EndsWith("\n"))
                {
                    row = row.Substring(0, row.Length - 1);
                }
                if (!regexFilter.IsMatch(row))
                {
                    // skip all input rows that do not match the input regex filter
                    startIndex0 = startIndex1;
                    ++lineNo;
                    continue;
                }

                if (numFields == 0)
                {
                    numFields = fields.Count;
                }
                
                if (fields.Count != numFields)
                    throw new Exception(
                        $"Found {fields.Count} fields instead of {numFields} fields in line {lineNo + 1}");

                RowFields.Add(fields);
                //Rows.Add(input.Substring(startIndex0, startIndex1 - startIndex0));
                Rows.Add(string.Join(fieldDelimiter.ToString(CultureInfo.InvariantCulture), fields)); // record without recordDelimiter

                startIndex0 = startIndex1;
                ++lineNo;
				// ReSharper disable once LoopVariableIsNeverChangedInsideLoop
            } while (sentinelIndex != startIndex1);
        }

        public override string ToString()
        {
            return $"NumFields: {NumFields}, NumRows: {NumRows}, Header: {Header}";
        }
    }
}
