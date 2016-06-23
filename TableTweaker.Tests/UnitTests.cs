using System.Collections.Generic;
using TableTweaker.Model;
using Xunit;

namespace TableTweaker.Tests
{
    public class UnitTests
    {
        #region Test Data

        private const char CommaFieldDelimitter = ',';

        private const bool QuotedFields = false;

        private const string MyCode = "public static string GetInitial(string s) { return s.Substring(0,1); }";

        private const string HeaderAndThreeRowsInput = 
            "Last Name,First Name,Company\r\n" +
            "Jobs,Steve,Apple\r\n" +
            "Cook,Tim,Apple\r\n" +
            "Gates,Bill,Microsoft\r\n";

        private const string HeaderAndThreeRowsInputWithQuotedFields =
            "\"Last Name\",\"First Name\",\"Company\"\r\n" +
            "\"Jobs\",\"Steve\",\"Apple\"\r\n" +
            "\"Cook\",\"Tim\",\"Apple\"\r\n" +
            "\"Gates\",\"William \"\"Bill\"\"\",\"Microsoft\"\r\n";

        private const string CopyPattern =
            "$0,$1,$2\r\n";

        private const string InvertedCopyPattern =
            "$-2,$-1,$-0\r\n";

        private const string AllTokenPattern =
            "Text,$dollar;\r\n" +
            "$h0,$h1,$h2\r\n" +
            "$h-0,$h-1,$h-2\r\n" +
            "$0,$1,$2\r\n" +
            "$-0,$-1,$-2\r\n" +
            "$header\r\n" +
            "$row\r\n" +
            "$rowNum,$rowNumOne,$numFields,$numRows\r\n" +
			"$GetInitial(\"$1\")\r\n" +
            "$ONCE\r\n" +
            "a\r\n" +
            "$EACH\r\n" +
            "b\r\n" +
            "$EACH+\r\n" +
            "c\r\n";

        private const string HeaderAndThreeRowsAllTokenPatternOutput =
            "Text,$\r\n" +
            "Last Name,First Name,Company\r\n" +
            "Company,First Name,Last Name\r\n" +
            "Last Name,First Name,Company\r\n" +
            "Company,First Name,Last Name\r\n" +
            "Last Name,First Name,Company\r\n" +
            "Last Name,First Name,Company\r\n" +
            "0,1,3,4\r\n" +
            "F\r\n" +
            "Text,$\r\n" +
            "Last Name,First Name,Company\r\n" +
            "Company,First Name,Last Name\r\n" +
            "Jobs,Steve,Apple\r\n" +
            "Apple,Steve,Jobs\r\n" +
            "Last Name,First Name,Company\r\n" +
            "Jobs,Steve,Apple\r\n" +
            "1,2,3,4\r\n" +
            "S\r\n" +
            "Text,$\r\n" +
            "Last Name,First Name,Company\r\n" +
            "Company,First Name,Last Name\r\n" +
            "Cook,Tim,Apple\r\n" +
            "Apple,Tim,Cook\r\n" +
            "Last Name,First Name,Company\r\n" +
            "Cook,Tim,Apple\r\n" +
            "2,3,3,4\r\n" +
            "T\r\n" +
            "Text,$\r\n" +
            "Last Name,First Name,Company\r\n" +
            "Company,First Name,Last Name\r\n" +
            "Gates,Bill,Microsoft\r\n" +
            "Microsoft,Bill,Gates\r\n" +
            "Last Name,First Name,Company\r\n" +
            "Gates,Bill,Microsoft\r\n" +
            "3,4,3,4\r\n" +
            "B\r\n" +
            "a\r\n" +
            "b\r\n" +
            "b\r\n" +
            "b\r\n" +
            "b\r\n" +
            "c\r\n" +
            "c\r\n" +
            "c\r\n";

        private readonly List<Token> _allTokenReference = new List<Token>
        {
            // 0
            new Token(TokenCategory.Text, "Text,"),
            new Token(TokenCategory.Dollar),
            new Token(TokenCategory.Text, "\r\n"),
            new Token(TokenCategory.HeaderIndex, "0"),
            new Token(TokenCategory.Text, ","),
            new Token(TokenCategory.HeaderIndex, "1"),
            new Token(TokenCategory.Text, ","),
            new Token(TokenCategory.HeaderIndex, "2"),
            new Token(TokenCategory.Text, "\r\n"),
            new Token(TokenCategory.InvertedHeaderIndex, "0"),
            new Token(TokenCategory.Text, ","),
            new Token(TokenCategory.InvertedHeaderIndex, "1"),

            // 10
            new Token(TokenCategory.Text, ","),
            new Token(TokenCategory.InvertedHeaderIndex, "2"),
            new Token(TokenCategory.Text, "\r\n"),
            new Token(TokenCategory.FieldIndex, "0"),
            new Token(TokenCategory.Text, ","),
            new Token(TokenCategory.FieldIndex, "1"),
            new Token(TokenCategory.Text, ","),
            new Token(TokenCategory.FieldIndex, "2"),
            new Token(TokenCategory.Text, "\r\n"),
            new Token(TokenCategory.InvertedFieldIndex, "0"),

            // 20
            new Token(TokenCategory.Text, ","),
            new Token(TokenCategory.InvertedFieldIndex, "1"),
            new Token(TokenCategory.Text, ","),
            new Token(TokenCategory.InvertedFieldIndex, "2"),
            new Token(TokenCategory.Text, "\r\n"),
            new Token(TokenCategory.Header),
            new Token(TokenCategory.Text, "\r\n"),
            new Token(TokenCategory.Row),
            
            // 30
            new Token(TokenCategory.Text, "\r\n"),
            new Token(TokenCategory.RowNum),
            new Token(TokenCategory.Text, ","),
            new Token(TokenCategory.RowNumOne),
            new Token(TokenCategory.Text, ","),
            new Token(TokenCategory.NumFields),
            new Token(TokenCategory.Text, ","),
            new Token(TokenCategory.NumRows),
            new Token(TokenCategory.Text, "\r\n"),
            new Token(TokenCategory.MethodCall, "GetInitial(\"$1\")"),
            
            // 40
            new Token(TokenCategory.Text, "\r\n"),
            new Token(TokenCategory.Once),
            new Token(TokenCategory.Text, "a\r\n"),
            new Token(TokenCategory.Each),
            new Token(TokenCategory.Text, "b\r\n"),
            new Token(TokenCategory.EachPlus),
            new Token(TokenCategory.Text, "c\r\n"),
            new Token(TokenCategory.EndOfInput)
        };

        #endregion TestData

        #region ScannerTests

        [Fact]
        [Trait("Category", "Scanner")]
        public void EmptyInputScannerTest()
        {
            var scanner = new Scanner("");
            var tokens = scanner.GetAllTokens();
            Assert.NotNull(tokens);
            Assert.Equal(1, tokens.Count);
            Assert.Equal(TokenCategory.EndOfInput, tokens[0].Category);
        }

        [Fact]
        [Trait("Category", "Scanner")]
        public void CopyPatternScannerTest()
        {
            var scanner = new Scanner(CopyPattern);
            var tokens = scanner.GetAllTokens();
            Assert.NotNull(tokens);
            Assert.Equal(7, tokens.Count);
            Assert.Equal(TokenCategory.FieldIndex, tokens[0].Category);
            Assert.Equal("0", tokens[0].Value);
            Assert.Equal(TokenCategory.Text, tokens[1].Category);
            Assert.Equal(",", tokens[1].Value);
            Assert.Equal(TokenCategory.FieldIndex, tokens[2].Category);
            Assert.Equal("1", tokens[2].Value);
            Assert.Equal(TokenCategory.Text, tokens[3].Category);
            Assert.Equal(",", tokens[3].Value);
            Assert.Equal(TokenCategory.FieldIndex, tokens[4].Category);
            Assert.Equal("2", tokens[4].Value);
            Assert.Equal(TokenCategory.Text, tokens[5].Category);
            Assert.Equal("\r\n", tokens[5].Value);
            Assert.Equal(TokenCategory.EndOfInput, tokens[tokens.Count - 1].Category);
        }

        [Fact]
        [Trait("Category", "Scanner")]
        public void FullPatternScannerTest()
        {
            var scanner = new Scanner(AllTokenPattern);
            var tokens = scanner.GetAllTokens();
            Assert.NotNull(tokens);
            //Assert.Equal(_allTokenReference.Count, tokens.Count);
            for (var i = 0; i < _allTokenReference.Count; ++i)
            {
                var token = tokens[i];
                Assert.Equal(_allTokenReference[i].Category, token.Category);
                Assert.Equal(_allTokenReference[i].Value, token.Value);
                Assert.Equal(_allTokenReference[i].Value, token.Value);
            }
        }

        #endregion ScannerTests

        #region CsvStringConverterTests

        [Fact]
        [Trait("Category", "CSV")]
        public void ConvertCsvTest()
        {
            const string input = "Vorname;Nachname;PLZ;Stadt;Straße;Nr\n" +
                                 ";a;\"a;\";\"a\r\n" +
                                 "\"\"b\";;\"\"\r\n" +
                                 "Michael;Heitland;31139;\"Hildesheim\";Trillkestraße;5\r\n" +
                                 "Karl;Müller;12345;Karlsruhe;\"\";\n";

            var expectedOutput = new[]
            {
                "Vorname",
                "Nachname",
                "PLZ",
                "Stadt",
                "Straße",
                "Nr",

                "",
                "a",
                "a;",
                "a\n\"b",
                "",
                "",

                "Michael",
                "Heitland",
                "31139",
                "Hildesheim",
                "Trillkestraße",
                "5",

                "Karl",
                "Müller",
                "12345",
                "Karlsruhe",
                "",
                ""
            };

            var csvStringConverter = new CsvStringConverter(';', true);
            var startIndex = 0;
            var sentinelIndex = input.Length;
            var output = new List<string>();

			// ReSharper disable once LoopVariableIsNeverChangedInsideLoop
            while (sentinelIndex !=	
		        (startIndex = csvStringConverter.ConvertCsv(input, startIndex, sentinelIndex, output)))
            {
                Assert.Equal(0, output.Count % 6); // invalid no of fields?
            }

            for (var i = 0; i < output.Count; ++i)
            {
                var currentOutput = output[i];
                var currentExpected = expectedOutput[i];
                Assert.Equal(currentOutput, currentExpected);
            }
        }

        #endregion CsvStringConverterTests

        #region TableTests

        [Fact]
        [Trait("Category", "Table")]
        public void TableTest()
        {
            var table = new Table(HeaderAndThreeRowsInput, CommaFieldDelimitter, QuotedFields, ".*");
            
            Assert.NotNull(table);
            Assert.Equal(3, table.NumFields);
            Assert.Equal(4, table.NumRows);
            
            Assert.Equal("Last Name,First Name,Company", table.Header);

            Assert.Equal("Last Name,First Name,Company", table.Rows[0]);
            Assert.Equal("Jobs,Steve,Apple", table.Rows[1]);
            Assert.Equal("Cook,Tim,Apple", table.Rows[2]);
            Assert.Equal("Gates,Bill,Microsoft", table.Rows[3]);

            Assert.Equal("Last Name", table.HeaderFields[0]);
            Assert.Equal("First Name", table.HeaderFields[1]);
            Assert.Equal("Company", table.HeaderFields[2]);

            Assert.Equal("Last Name", table.RowFields[0][0]);
            Assert.Equal("First Name", table.RowFields[0][1]);
            Assert.Equal("Company", table.RowFields[0][2]);

            Assert.Equal("Jobs", table.RowFields[1][0]);
            Assert.Equal("Steve", table.RowFields[1][1]);
            Assert.Equal("Apple", table.RowFields[1][2]);

            Assert.Equal("Cook", table.RowFields[2][0]);
            Assert.Equal("Tim", table.RowFields[2][1]);
            Assert.Equal("Apple", table.RowFields[2][2]);

            Assert.Equal("Gates", table.RowFields[3][0]);
            Assert.Equal("Bill", table.RowFields[3][1]);
            Assert.Equal("Microsoft", table.RowFields[3][2]);
        }

        [Fact]
        [Trait("Category", "Table")]
        public void TableTestWithQuotedFields()
        {
            var table = new Table(HeaderAndThreeRowsInputWithQuotedFields, CommaFieldDelimitter, true, ".*");

            Assert.NotNull(table);
            Assert.Equal(3, table.NumFields);
            Assert.Equal(4, table.NumRows);

            Assert.Equal("Last Name,First Name,Company", table.Header);

            Assert.Equal("Last Name,First Name,Company", table.Rows[0]);
            Assert.Equal("Jobs,Steve,Apple", table.Rows[1]);
            Assert.Equal("Cook,Tim,Apple", table.Rows[2]);
            Assert.Equal("Gates,William \"Bill\",Microsoft", table.Rows[3]);

            Assert.Equal("Last Name", table.HeaderFields[0]);
            Assert.Equal("First Name", table.HeaderFields[1]);
            Assert.Equal("Company", table.HeaderFields[2]);

            Assert.Equal("Last Name", table.RowFields[0][0]);
            Assert.Equal("First Name", table.RowFields[0][1]);
            Assert.Equal("Company", table.RowFields[0][2]);

            Assert.Equal("Jobs", table.RowFields[1][0]);
            Assert.Equal("Steve", table.RowFields[1][1]);
            Assert.Equal("Apple", table.RowFields[1][2]);

            Assert.Equal("Cook", table.RowFields[2][0]);
            Assert.Equal("Tim", table.RowFields[2][1]);
            Assert.Equal("Apple", table.RowFields[2][2]);

            Assert.Equal("Gates", table.RowFields[3][0]);
            Assert.Equal("William \"Bill\"", table.RowFields[3][1]);
            Assert.Equal("Microsoft", table.RowFields[3][2]);
        }

        #endregion TableTests

        #region EngineTests

        [Fact]
        [Trait("Category", "Engine")]
        public void EmptyInputEngineTest()
        {
            var engine = Engine.Instance;
            var input = "\r\n";
            var table = new Table(input, CommaFieldDelimitter, QuotedFields, "");
            var output = engine.Process(table, "", "");
            Assert.Equal(input, output);
        }

        [Fact]
        [Trait("Category", "Engine")]
        public void NoRulesEngineTest()
        {
            var engine = Engine.Instance;
            var table = new Table(HeaderAndThreeRowsInput, CommaFieldDelimitter, QuotedFields, "");
            var output = engine.Process(table, "", "");
            Assert.Equal(HeaderAndThreeRowsInput, output);
        }

        [Fact]
        [Trait("Category", "Engine")]
        public void CopyPatternEngineTest()
        {
            var engine = Engine.Instance;
            var table = new Table(HeaderAndThreeRowsInput, CommaFieldDelimitter, QuotedFields, "");
            var output = engine.Process(table, CopyPattern, "");
            Assert.Equal(HeaderAndThreeRowsInput, output);
        }

        [Fact]
        [Trait("Category", "Engine")]
        public void InvertedCopyPatternEngineTest()
        {
            var engine = Engine.Instance;
            var table = new Table(HeaderAndThreeRowsInput, CommaFieldDelimitter, QuotedFields, "");
            var output = engine.Process(table, InvertedCopyPattern, "");
            Assert.Equal(HeaderAndThreeRowsInput, output);
        }

        [Fact]
        [Trait("Category", "Engine")]
        public void AllTokenPatternEngineTest()
        {
            var engine = Engine.Instance;
            var table = new Table(HeaderAndThreeRowsInput, CommaFieldDelimitter, QuotedFields, "");
            var output = engine.Process(table, AllTokenPattern, MyCode);
            Assert.Equal(HeaderAndThreeRowsAllTokenPatternOutput, output);
        }

        [Fact]
        [Trait("Category", "Engine")]
        public void CopyQuotationMarkEngineTest()
        {
            var engine = Engine.Instance;
            var table = new Table("A\"B", CommaFieldDelimitter, false, "");
            var output = engine.Process(table, "$0", MyCode);
            Assert.Equal("A\"B", output);
        }

        [Fact]
        [Trait("Category", "Engine")]
        public void ProcessQuotationMarkEngineTest()
        {
            var engine = Engine.Instance;
            var table = new Table("A\"B", CommaFieldDelimitter, false, "");
            var output = engine.Process(table, "$ToLower(\"$0\")", "public static string ToLower(string s) { return s.ToLower(); }");
            Assert.Equal("a\"b", output);
        }

        #endregion EngineTests
    }
}
