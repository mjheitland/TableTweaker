using System.Collections.Generic;

namespace TableTweaker.Model
{
    public class CsvStringConverter
	{
		#region Constants
		
		private const char QuotationMarkChar = '\"';

        private const char RecordDelimiter = '\n';
		
		#endregion Constatnts

		#region Fields
		
		private string _currentField = "";

        private readonly char _fieldDelimiter;

        private readonly bool _quotedFields;

        #endregion Fields

		#region Constructor

		public CsvStringConverter(char fieldDelimiter, bool quotedFields)
        {
            _fieldDelimiter = fieldDelimiter;
		    _quotedFields = quotedFields;
        }

		#endregion Constructor

		#region Public Methods

		/// <summary>
        /// Splits next record (i.e. line) of 'input' in the range [startIndex .. sentinelIndex) into fields
        /// </summary>
        /// <param name="input"></param>
        /// <param name="startIndex"></param>
        /// <param name="sentinelIndex"></param>
        /// <param name="fields"></param>
        /// <returns>index where the next record starts</returns>
        public int ConvertCsv(string input, int startIndex, int sentinelIndex, List<string> fields)
        {
            var insideQuotes = false;
            var i = startIndex;
            var ch = '\0';

            // convert one record to list of fields
            while (true)
            {
                // add normal chars to currentField
                while (i < sentinelIndex)
                {
                    ch = input[i];
                    if ((_quotedFields && ch == QuotationMarkChar) ||
                        ch == _fieldDelimiter ||
                        ch == RecordDelimiter)
                        break;

                    if (input[i] != '\r')
                    {
                        // we are always ignoring '\r'
                        _currentField += input[i];
                    }
                    ++i;
                }

                if (i == sentinelIndex)
                    break;

                if (_quotedFields && ch == QuotationMarkChar)
                {
                    if (insideQuotes && i + 1 < sentinelIndex && input[i + 1] == QuotationMarkChar)
                    {
                        // "a""b" -> a"b
                        //   ^
                        _currentField += QuotationMarkChar;
                        ++i; // skip first QUOTATION_MARK_CHAR
                        // "a""b" -> a"b
                        //    ^
                    }
                    else
                    {
                        // we are ignoring the outer quotation marks: "ab" -> ab
                        insideQuotes = !insideQuotes;
                    }
                }
                else if (ch == _fieldDelimiter)
                {
                    if (insideQuotes)
                    {
                        _currentField += _fieldDelimiter;
                    }
                    else
                    {
                        fields.Add(_currentField);
                        _currentField = "";
                    }
                }
                else if (ch == RecordDelimiter)
                {
                    if (insideQuotes)
                    {
                        _currentField += RecordDelimiter;
                    }
                    else
                    {
                        ++i; // skip record delimiter
                        break;
                    }
                }

                ++i;
            }

            // add field of last column: here we do not have a field delimiter but a row delimiter or end of string
            fields.Add(_currentField);
            _currentField = "";

            return i;
        }

		#endregion Public Methods
	}
}