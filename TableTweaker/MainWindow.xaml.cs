using System;
using System.Collections;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using ICSharpCode.AvalonEdit.Highlighting;
using TableTweaker.Editor;
using TableTweaker.Model;
using TableTweaker.Properties;
using TableTweaker.Roslyn;
using TableTweaker.Utilities;
using Table = TableTweaker.Model.Table;

namespace TableTweaker
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow
    {
        private readonly Engine _engine = Engine.Instance;

        private bool _autoMode;

        private readonly bool _windowIsInitialized;

        private readonly Stopwatch _stopwatch = new Stopwatch();

        private readonly Style _paragraphStyle;

        // ReSharper disable once PrivateFieldCanBeConvertedToLocalVariable
        private readonly InteractiveManager _interactiveManager;

        public MainWindow()
        {
            InitializeComponent();

            _paragraphStyle = Resources["ParagraphWithNoMarginStyle"] as Style;

            ConfigureEditor();

            ManageUserSettings();

            _interactiveManager = new InteractiveManager();
            _interactiveManager.SetDocument(Editor.AsTextContainer());
            Editor.CompletionProvider = new RoslynCodeEditorCompletionProvider(_interactiveManager);

            var helpContent = File.ReadAllText(@"Content\help.html");
            Help.NavigateToString(helpContent);

            TbxInput.Focus();

            _windowIsInitialized = true;

            Process(); 
        }

        private void MainWindow_OnLoaded(object sender, RoutedEventArgs e)
        {
        }

        private void BtnRun_Click(object sender, RoutedEventArgs e)
        {
            Process();
        }

        private void Process()
        {
            try
            {
                if (!_windowIsInitialized)
                    return; // called during initialization!

                using (new WaitCursor()) { 
                    var filter = CbxFilters.SelectedValue?.ToString() ?? CbxFilters.Text;
                    if (string.IsNullOrEmpty(filter))
                    {
                        filter = ".*";
                    }

                    var input = GetText(TbxInput);
                    var table = new Table(input, _engine.FieldDelimiter, _engine.QuotedFields, filter);

                    var pattern = new TextRange(TbxPattern.Document.ContentStart, TbxPattern.Document.ContentEnd).Text;

                    var code = Editor.Text;

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

        private void TbxPattern_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (!_windowIsInitialized)
                return; // called during initialization!

            if (_autoMode)
            {
                Process();
            }

            SetParagraphStyle(TbxPattern);
        }

        private void TbxInput_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (!_windowIsInitialized)
                return; // called during initialization!

            if (_autoMode)
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

        private void CbxMode_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            _autoMode = CbxMode.SelectedIndex == 1;
        }

        private void CbxMacros_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!_windowIsInitialized)
                return; // called during initialization!

            if (CbxMacros.SelectedIndex == -1)
                return;

            var macro = CbxMacros.SelectedValue.ToString();
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

            var method = CbxMethods.SelectedValue.ToString();
            if (string.IsNullOrWhiteSpace(method))
                return;

            TbxPattern.CaretPosition.InsertTextInRun(method);

            CbxMethods.SelectedIndex = -1;
        }

        private void ManageUserSettings()
        {
            CbxDelimiter.SelectedIndex = Settings.Default.CbxDelimiteIndex;
            CbxFilters.SelectedValue = Settings.Default.CbxFiltersValue;
            CbxFontSize.SelectedIndex = Settings.Default.CbxFontSizeIndex;
            CbxLineWrap.SelectedIndex = Settings.Default.CbxLineWrapIndex;
            CbxMode.SelectedIndex = Settings.Default.CbxModeIndex;
            CbxQualifier.SelectedIndex = Settings.Default.CbxQualifierIndex;
            CbxResultGridColumns.SelectedIndex = Settings.Default.CbxResultGridColumnsIndex;

            _engine.FieldDelimiter = CbxDelimiter.SelectedValue.ToString()[0];
            _engine.QuotedFields = CbxQualifier.SelectedValue.ToString().ToLower() == "\"";

            SetText(TbxPattern, Settings.Default.LastSessionPattern);
            SetText(TbxInput, Settings.Default.LastSessionInput);
            Editor.Text = Settings.Default.LastSessionCode;

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
                Settings.Default.LastSessionCode = Editor.Text;

                Settings.Default.Save();
            };
        }

        #region Code Editor

        private void ConfigureEditor()
        {
            Editor.SyntaxHighlighting = HighlightingManager.Instance.GetDefinition("C#");
        }

        private void OnPlayCommand(object sender, RoutedEventArgs e)
        {
            try
            {
                if (TbcMain.SelectedIndex == 1) // Code tab
                {
                    _interactiveManager.Execute();
                    MessageBox.Show("Code is correct!", "TableTweaker");
                } else // not on Code tab
                {
                    Process();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), "TableTweaker");
            }
        }

        #endregion Code Editor

        private void CbxDelimiter_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!_windowIsInitialized)
                return; // called during initialization!

            var delimiter = CbxDelimiter.SelectedValue.ToString();
            if (delimiter == "TAB")
            {
                delimiter = "\t";
            }
            if (delimiter.Length != 1)
                return;

            _engine.FieldDelimiter = delimiter[0];
            Process();
        }

        private void CbxQualifier_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!_windowIsInitialized)
                return; // called during initialization!

            _engine.QuotedFields = CbxQualifier.SelectedValue.ToString() == "\"";
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

            var fontSize = CbxFontSize.SelectedValue.ToString();
            if (string.IsNullOrWhiteSpace(fontSize))
                return;

            FontSize = int.Parse(fontSize);
        }

        private void CbxLineWrap_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!_windowIsInitialized)
                return;

            var value = CbxLineWrap.SelectedValue.ToString().ToLower();
            if (value == "no")
            {
                FldInput.PageWidth = 10000.0;
                FldOutput.PageWidth = 10000.0;
            }
            else
            {
                FldInput.PageWidth = double.NaN;
                FldOutput.PageWidth = double.NaN;
            }
        }

        private void CbxResultGridColumns_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!_windowIsInitialized)
                return;

            LoadInputAndOutputIntoResultGrid(CbxResultGridColumns.SelectedIndex + 1);
        }

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

    class ResultObject
    {
        private readonly object _o;
        private readonly PropertyDescriptor _property;
        private bool _initialized;
        private string _header;
        private IEnumerable _children;

        public ResultObject(object o, PropertyDescriptor property = null)
        {
            _o = o;
            _property = property;
        }

        public string Header
        {
            get
            {
                Initialize();
                return _header;
            }
        }

        public IEnumerable Children
        {
            get
            {
                Initialize();
                return _children;
            }
        }

        private void Initialize()
        {
            if (_initialized) return;
            _initialized = true;

            if (_o == null)
            {
                _header = "<null>";
                return;
            }

            if (_property != null)
            {
                var value = _property.GetValue(_o);
                _header = _property.Name + " = " + value;
                _children = new[] { value };
                return;
            }

            var e = _o as IEnumerable;
            if (e != null)
            {
                var enumerableChildren = e.Cast<object>().Select(x => new ResultObject(x)).ToArray();
                _children = enumerableChildren;
                _header = $"<enumerable count={enumerableChildren.Length}>";
                return;
            }

            var properties = TypeDescriptor.GetProperties(_o).Cast<PropertyDescriptor>()
                .Select(p => new ResultObject(_o, p)).ToArray();
            _header = _o.ToString();
            if (properties.Length > 0)
            {
                _children = properties;
            }
        }
    }
}
