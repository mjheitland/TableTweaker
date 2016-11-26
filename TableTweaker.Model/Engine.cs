// To use C# Scripting import nuget package Microsoft.CodeAnalysis.CSharp.Scripting
// https://blogs.msdn.microsoft.com/cdndevs/2015/12/01/adding-c-scripting-to-your-development-arsenal-part-1/
using System;
using System.Text;
using System.Xml;
using Microsoft.CodeAnalysis.CSharp.Scripting;
using Microsoft.CodeAnalysis.Scripting;

namespace TableTweaker.Model
{
    public class Engine : IEngine
    {
        #region Singleton

        // static holder for instance, need to use lambda to construct since constructor private
        // ReSharper disable once InconsistentNaming
        private static readonly Lazy<Engine> _instance = new Lazy<Engine>(() => new Engine());

        // private to prevent direct instantiation.
        private Engine()
        {
        }

        // accessor for instance
        public static Engine Instance => _instance.Value;

        #endregion Singleton

        private readonly ScriptOptions _scriptOptions = ScriptOptions.Default
            .AddReferences(typeof(XmlDocument).Assembly)
            .AddImports("System.Xml", "System");

        public char FieldDelimiter { get; set; } = ',';

        public bool QuotedFields { get; set; } = true;

        public string Process(Table table, string pattern, string code)
        {
            var output = new StringBuilder();

            if (table.NumRows == 0)
                return "";

            if (string.IsNullOrWhiteSpace(pattern))
                pattern = "$EACH\r\n$row\r\n";

            var scanner = new Scanner(pattern);
            var tokens = scanner.GetAllTokens();

            // Add EACH as default section token if 'tokens' does not start with any section token
            if (tokens[0].Category != TokenCategory.Each &&
                tokens[0].Category != TokenCategory.EachPlus &&
                tokens[0].Category != TokenCategory.Once)
            {
                tokens.Insert(0, new Token(TokenCategory.Each));
            }

            var tokenList = new TokenList(tokens);
            do
            {
                int rowNoStart;
                int rowNoSentinel;
                MapSectionTypeToRowNumbers(tokenList, table, out rowNoStart, out rowNoSentinel);

                ProcessSection(table, rowNoStart, rowNoSentinel, tokenList, code, output);
            } while (tokenList.Current.Category != TokenCategory.EndOfInput);

            return output.ToString();
        }

        private static void MapSectionTypeToRowNumbers(
            TokenList tokenList,
            Table table,
            out int rowNoStart,
            out int rowNoSentinel)
        {
            switch (tokenList.Values[tokenList.Index++].Category)
            {
                case TokenCategory.Once:
                    rowNoStart = 0;
                    rowNoSentinel = 1;
                    break;
                case TokenCategory.Each:
                    rowNoStart = 0;
                    rowNoSentinel = table.NumRows;
                    if (rowNoSentinel == 0)
                        throw new Exception("Input is empty, but EACH needs at least one row for processing!");
                    break;
                case TokenCategory.EachPlus:
                    rowNoStart = 1;
                    rowNoSentinel = table.NumRows;
                    if (rowNoSentinel == 1)
                        throw new Exception("Input is empty or has only one row, but EACH needs at least two rows for processing!");
                    break;
                default:
                    throw new Exception("Section token ($ONCE, $EACH, $EACH+) expected");
            }
        }

        private void ProcessSection(Table table, int rowNoStart, int rowNoSentinel, TokenList tokens, string code, StringBuilder output)
        {
            var tokensSectionStartIndex = tokens.Index;
            for (var rowNo = rowNoStart; rowNo < rowNoSentinel; ++rowNo)
            {
                var endOfSection = false;
                tokens.Index = tokensSectionStartIndex; // reset to 1st token in section 
                while (true)
                {
                    var token = tokens.Values[tokens.Index++];

                    string value;
                    int colIndex;
                    switch (token.Category)
                    {
                        case TokenCategory.Text:
                            output.Append(token.Value);
                            break;

                        case TokenCategory.Dollar:
                            output.Append("$");
                            break;

                        case TokenCategory.HeaderIndex:
                            colIndex = int.Parse(token.Value);
                            CheckColIndex(table, colIndex);
                            value = table.RowFields[0][colIndex];
                            output.Append(value);
                            break;
                        case TokenCategory.InvertedHeaderIndex:
                            colIndex = table.NumFields - 1 - int.Parse(token.Value);
                            CheckColIndex(table, colIndex);
                            value = table.RowFields[0][colIndex];
                            output.Append(value);
                            break;
                        case TokenCategory.FieldIndex:
                            colIndex = int.Parse(token.Value);
                            CheckColIndex(table, colIndex);
                            value = table.RowFields[rowNo][colIndex];
                            output.Append(value);
                            break;
                        case TokenCategory.InvertedFieldIndex:
                            colIndex = table.NumFields - 1 - int.Parse(token.Value);
                            CheckColIndex(table, colIndex);
                            value = table.RowFields[rowNo][colIndex];
                            output.Append(value);
                            break;

                        case TokenCategory.Header:
                            output.Append(table.Header);
                            break;
                        case TokenCategory.Row:
                            output.Append(table.Rows[rowNo]);
                            break;
                        case TokenCategory.RowNum:
                            output.Append(rowNo);
                            break;
                        case TokenCategory.RowNumOne:
                            output.Append(rowNo + 1);
                            break;
                        case TokenCategory.NumFields:
                            output.Append(table.NumFields);
                            break;
                        case TokenCategory.NumRows:
                            output.Append(table.NumRows);
                            break;

                        case TokenCategory.MethodCall:
                            var pos = token.Value.IndexOfAny("([{<".ToCharArray());
                            var methodName = token.Value.Substring(0, pos);
                            var args = token.Value.Substring(pos + 1, token.Value.Length - pos - 2);

                            // process args
                            var argsOutput = new StringBuilder();
                            var argsScanner = new Scanner(args);
                            var argsTokens = new TokenList(argsScanner.GetAllTokens());
                            ProcessSection(table, rowNo, rowNo + 1, argsTokens, "", argsOutput);
                            var encodedArgs = EncodeQuotationMark(argsOutput.ToString());

                            var methodCall = $"{methodName}({encodedArgs})"; // without terminating ";" to signal scripting engine that it should return the value!

                            var result = ScriptCSharpCode(code + methodCall);

                            output.Append(result);
                            break;

                        case TokenCategory.Once:
                        case TokenCategory.Each:
                        case TokenCategory.EachPlus:
                        case TokenCategory.EndOfInput:
                            tokens.Index--;
                            endOfSection = true;
                            break;

                        default:
                            throw new Exception("No code implemented for token.Category=" + token.Category);
                    }

                    if (endOfSection)
                        break;
                }
            }
        }

        private static string EncodeQuotationMark(string s)
        {
            if (string.IsNullOrEmpty(s) || s.Length < 2 || s[0] != '\"' || s[s.Length - 1] != '\"')
                throw new Exception($"Internal error, invalid args ({s ?? ""})");

            return $"\"{s.Substring(1, s.Length - 2).Replace("\"", "\\\"")}\"";
        }

        private static void CheckColIndex(Table table, int colIndex)
        {
            if (colIndex < 0)
            {
                throw new Exception("Column index $i can not be negative!");
            }
            if (colIndex >= table.NumFields)
            {
                throw new Exception(
                    $"Column index ${colIndex} is 0-based and cannot be larger than $numFields ({table.NumFields})!");
            }
        }

        public string ScriptCSharpCode(string code)
        {
            var result = CSharpScript.EvaluateAsync<string>(code, _scriptOptions).Result;
            return result;
        }

        public override string ToString()
        {
            return $"FieldDelimiter: '{FieldDelimiter}', QuotedFields: {QuotedFields}";
        }
    }
}
