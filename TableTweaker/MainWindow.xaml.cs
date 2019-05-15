//#pragma warning disable nullable

using RoslynPad.Editor;
using RoslynPad.Roslyn;
using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Reflection;
using System.Windows;
using TableTweaker.Model;
using System.Diagnostics;
using System.Windows.Controls;
using System.Windows.Documents;
using TableTweaker.Properties;
using System.Linq;
using TableTweaker.Utilities;

using Table = TableTweaker.Model.Table;

namespace TableTweaker
{

    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        #region Private Fields

        private readonly Engine _engine = Engine.Instance;

        private readonly Stopwatch _stopwatch = new Stopwatch();

        private readonly Style _paragraphStyle;

        private readonly ObservableCollection<DocumentViewModel> _documents;

        private readonly RoslynHost _host;

        private bool _windowIsInitialized = false;

        #endregion Private Fields

        #region Properties

        public char FieldDelimiter => CbxDelimiter.SelectedValue.ToString().Length == 1 ? CbxDelimiter.SelectedValue.ToString()[0] : '\t';

        public string Filter => CbxFilters.SelectedValue?.ToString() ?? CbxFilters.Text;

        public int SelectedFontSize => int.Parse(CbxFontSize.SelectedValue?.ToString() ?? "12");

        public bool IsAutoMode => CbxMode.SelectedIndex == 1;

        public string Macro => CbxMacros.SelectedValue?.ToString() ?? "";

        public string Method => CbxMethods.SelectedValue?.ToString() ?? "";

        public double PageWidth => CbxLineWrap.SelectedIndex == 0 ? 10000.0 : double.NaN;

        public bool QuotedFields => (CbxQualifier.SelectedValue?.ToString().ToLower() ?? "") == "\"";

        #endregion Properties

        public MainWindow()
        {
            InitializeComponent();

            Loaded += OnLoaded;

            _paragraphStyle = Resources["ParagraphWithNoMarginStyle"] as Style;

            _documents = new ObservableCollection<DocumentViewModel> { new DocumentViewModel(_host, null) };

            _host = new RoslynHost(additionalAssemblies: new[]
            {
                Assembly.Load("RoslynPad.Roslyn.Windows"),
                Assembly.Load("RoslynPad.Editor.Windows")
            });
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            Loaded -= OnLoaded;

            ManageUserSettings();

            var helpContent = File.ReadAllText(@"Content\help.html");
            Help.NavigateToString(helpContent);

            TbxInput.Focus();
        }

        private void Editor_OnLoaded(object sender, EventArgs e)
        {
            if (_windowIsInitialized)
                return;

            MyCodeEditor.DataContext = _documents[0];
            var viewModel = (DocumentViewModel)MyCodeEditor.DataContext;
            var workingDirectory = Directory.GetCurrentDirectory();
            var documentId = MyCodeEditor.Initialize(
                _host,
                new ClassificationHighlightColors(),
                workingDirectory,
                Settings.Default.LastSessionCode);
            viewModel.Initialize(documentId);

            _windowIsInitialized = true;

            Process();
        }

        #region Methods

        private void ManageUserSettings()
        {
            CbxDelimiter.SelectedIndex = Settings.Default.CbxDelimiteIndex;
            CbxFilters.SelectedValue = Settings.Default.CbxFiltersValue;
            CbxFontSize.SelectedIndex = Settings.Default.CbxFontSizeIndex;
            CbxLineWrap.SelectedIndex = Settings.Default.CbxLineWrapIndex;
            CbxMode.SelectedIndex = Settings.Default.CbxModeIndex;
            CbxQualifier.SelectedIndex = Settings.Default.CbxQualifierIndex;
            CbxResultGridColumns.SelectedIndex = Settings.Default.CbxResultGridColumnsIndex;

            _engine.FieldDelimiter = FieldDelimiter;
            _engine.QuotedFields = QuotedFields;

            SetText(TbxPattern, Settings.Default.LastSessionPattern);
            SetText(TbxInput, Settings.Default.LastSessionInput);
            //MyCodeEditor.Text = Settings.Default.LastSessionCode;

            Application.Current.Exit += (sender, args) =>
            {
                Settings.Default.CbxDelimiteIndex = CbxDelimiter.SelectedIndex;
                Settings.Default.CbxFiltersValue = CbxFilters.SelectedValue.ToString();
                Settings.Default.CbxFontSizeIndex = CbxFontSize.SelectedIndex;
                Settings.Default.CbxLineWrapIndex = CbxLineWrap.SelectedIndex;
                Settings.Default.CbxModeIndex = CbxMode.SelectedIndex;
                Settings.Default.CbxQualifierIndex = CbxQualifier.SelectedIndex;
                Settings.Default.CbxResultGridColumnsIndex = CbxResultGridColumns.SelectedIndex;

                Settings.Default.LastSessionPattern = new TextRange(TbxPattern.Document.ContentStart, TbxPattern.Document.ContentEnd).Text;
                Settings.Default.LastSessionInput = new TextRange(TbxInput.Document.ContentStart, TbxInput.Document.ContentEnd).Text;
                Settings.Default.LastSessionCode = MyCodeEditor.Text;

                Settings.Default.Save();
            };
        }

        private void Process()
        {
            try
            {
                if (!_windowIsInitialized)
                    return; // called during initialization!

                using (new WaitCursor())
                {
                    var filter = Filter;
                    if (string.IsNullOrEmpty(filter))
                    {
                        filter = ".*";
                    }

                    var input = GetText(TbxInput);
                    var table = new Table(input, _engine.FieldDelimiter, _engine.QuotedFields, filter);

                    var pattern = new TextRange(TbxPattern.Document.ContentStart, TbxPattern.Document.ContentEnd).Text;

                    var code = MyCodeEditor.Text;

                    _stopwatch.Reset();
                    _stopwatch.Start();
                    var output = _engine.Process(table, pattern, code).Replace("\r", "");
                    _stopwatch.Stop();

                    SetText(TbxOutput, output);

                    TblMessage.Text =
                        $"{TbxInput.Document.Blocks.Count} unfiltered input rows, {table.NumRows} filtered input rows, {TbxOutput.Document.Blocks.Count} output rows ({_stopwatch.ElapsedMilliseconds} ms)";
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, @"TableTweaker");
            }
        }

        private string GetText(RichTextBox tbx)
        {
            return new TextRange(tbx.Document.ContentStart, tbx.Document.ContentEnd).Text.Replace("\r", "");
        }

        private void SetText(RichTextBox richTextBox, string text)
        {
            richTextBox.Document.Blocks.Clear();

            if (string.IsNullOrEmpty(text))
                return;

            if (text.EndsWith("\n"))
            {
                // remove last "\n" so that we are not inserting an empty trailing paragraph into the RichTextbox
                text = text.Substring(0, text.Length - 1);
            }

            var lines = text.Replace("\r", "").Split("\n".ToCharArray());
            lines
                .Select(line =>
                new Paragraph(new Run(line))
                {
                    Style = _paragraphStyle
                })
                .ToList()
                .ForEach(richTextBox.Document.Blocks.Add);
        }

        #endregion Methods

        #region Event Handler

        private void OnPlayCommand(object sender, RoutedEventArgs e)
        {
            try
            {
                if (TbcMain.SelectedIndex == 1) // Code tab
                {
                    // TODO
                    //_interactiveManager.Execute();
                    MessageBox.Show("Code is correct!", "TableTweaker");
                }
                else // not on Code tab
                {
                    Process();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), "TableTweaker");
            }
        }

        private void BtnRun_Click(object sender, RoutedEventArgs e)
        {
            Process();
        }

        private void TbxPattern_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (!_windowIsInitialized)
                return; // called during initialization!

            if (IsAutoMode)
            {
                Process();
            }

            SetParagraphStyle(TbxPattern);
        }

        private void TbxInput_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (!_windowIsInitialized)
                return; // called during initialization!

            if (IsAutoMode)
            {
                Process();
            }
            else
            {
                TblMessage.Text = $"{TbxInput.Document.Blocks.Count} unfiltered input rows";
            }

            SetParagraphStyle(TbxInput);
        }

        private void SetParagraphStyle(RichTextBox richTextBox)
        {
            foreach (var paragraph in richTextBox.Document.Blocks)
            {
                paragraph.Style = _paragraphStyle;
            }
        }

        private void CbxMacros_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!_windowIsInitialized)
                return; // called during initialization!

            if (CbxMacros.SelectedIndex == -1)
                return;

            var macro = Macro;
            if (string.IsNullOrWhiteSpace(macro))
                return;

            if (macro == "$ONCE" || macro == "$EACH" || macro == "$EACH+")
            {
                macro += "\n";
            }

            TbxPattern.CaretPosition.InsertTextInRun(macro);

            CbxMacros.SelectedIndex = -1;
        }

        private void CbxMethods_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!_windowIsInitialized)
                return; // called during initialization!

            if (CbxMethods.SelectedIndex == -1)
                return;

            var method = Method;
            if (string.IsNullOrWhiteSpace(method))
                return;

            TbxPattern.CaretPosition.InsertTextInRun(method);

            CbxMethods.SelectedIndex = -1;
        }

        private void CbxDelimiter_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!_windowIsInitialized)
                return; // called during initialization!

            _engine.FieldDelimiter = FieldDelimiter;
            Process();
        }

        private void CbxQualifier_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!_windowIsInitialized)
                return; // called during initialization!

            _engine.QuotedFields = QuotedFields;
            Process();
        }

        private void TbcMain_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            BtnRun.Content = (TbcMain.SelectedIndex == 2) ? "Check (F5)" : "Run (F5)";
            if (TbcMain.SelectedIndex == 1)
            {
                LoadInputAndOutputIntoResultGrid(CbxResultGridColumns.SelectedIndex + 1);
            }
        }

        private void CbxFontSize_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!_windowIsInitialized)
                return;

            FontSize = SelectedFontSize;
        }

        private void CbxLineWrap_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!_windowIsInitialized)
                return;

            FldInput.PageWidth = PageWidth;
            FldOutput.PageWidth = PageWidth;
        }

        private void CbxResultGridColumns_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!_windowIsInitialized)
                return;

            LoadInputAndOutputIntoResultGrid(CbxResultGridColumns.SelectedIndex + 1);
        }

        #endregion Event Handler

        #region Result

        private string CreateOneColumnRow(string input, string output)
        {
            return $"<tr style=\"background-color: #ddd;\"><td>{input}</td></tr>"
                + $"<tr style=\"background-color: #fff;\"><td>{output}</td></tr>";
        }

        private string CreateTwoColumnRow(string input, string output, bool darker)
        {
            return "<tr style=\"background-color: " + (darker ? "#ddd" : "#fff") + $";\"><td>{input}</td><td>{output}</td></tr>";
        }

        private void LoadInputAndOutputIntoResultGrid(int numColumns)
        {
            var inputLines = GetText(TbxInput).Split("\n".ToCharArray());
            var outputLines = GetText(TbxOutput).Split("\n".ToCharArray());

            string rows;
            if (numColumns == 1)
            {
                rows = inputLines
                    .Select((inputLine, i) => new[] { inputLine, i < outputLines.Length ? outputLines[i] : "" })
                    .Select((linePair, i) => CreateOneColumnRow(linePair[0], linePair[1]))
                    .Aggregate((a, b) => a + b);
            }
            else
            {
                rows = inputLines
                    .Select((inputLine, i) => new[] { inputLine, i < outputLines.Length ? outputLines[i] : "" })
                    .Select((linePair, i) => CreateTwoColumnRow(linePair[0], linePair[1], i % 2 == 0))
                    .Aggregate((a, b) => a + b);
            }

            var content =
                $@"
<!DOCTYPE html>
<html>
<head>
    <link rel=""stylesheet"" href=""https://maxcdn.bootstrapcdn.com/bootstrap/3.3.6/css/bootstrap.min.css"" crossorigin=""anonymous"">
</head>
<body>
    <div class=""container"">
        <table class=""table table-striped"">
        <thead></thead>
        <tbody>
            {rows}
        </tbody>
        </table>
    </div>
</body>
</html>";

            if (numColumns == 2)
            {
                content = content.Replace("<thead></thead>", "<thead><th>Input</th><th>Output</th></thead>");
            }

            MyWebBrowser.NavigateToString(content);
        }

        #endregion // Result
    }
}
