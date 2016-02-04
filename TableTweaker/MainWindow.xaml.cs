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

        private bool _autoMode = true;

        private readonly bool _windowIsInitialized;

        private readonly Stopwatch _stopwatch = new Stopwatch();

        private readonly Style _paragraphStyle;

        private const string DefaultLastSessionPattern =
            "$EACH+\r\n" +
            "$rowNum\r\n" +
            "To: $ToLower(\"$1.$0@$2.com\")\r\n" +
            "Hello $1 $0,\r\n" +
            "I'm sorry to inform you of a terrible accident at $2.\r\n" +
            "---\r\n";

        private const string DefaultLastSessionInput =
            "Last Name,First Name, Company\r\n" +
            "Cook,Tim,Apple\r\n" +
            "Nadella,Satya,Microsoft\r\n" +
            "Drury,Rod,Xero\r\n" +
            "Zuckerberg,Mark,Facebook\r\n" +
            "Page,Larry,Google\r\n";

        private const string DefaultLastSessionCode =
            "using System;\r\n" +
            "public string FormatDate(string date, string format)\r\n" +
            "{\r\n" +
            "\treturn DateTime.Parse(date).ToString(format);\r\n" +
            "}\r\n" +
            "public string IndexOf(string s, string value)\r\n" +
            "{\r\n" +
            "\treturn s.IndexOf(value).ToString();\r\n" +
            "}\r\n" +
            "public string Left(string s, int length)\r\n" +
            "{\r\n" +
            "\treturn string.IsNullOrEmpty(s) ? string.Empty : s.Substring(0, (length < s.Length ) ? length : s.Length);\r\n" +
            "}\r\n" +
            "public string Right(string s, int length)\r\n" +
            "{\r\n" +
            "\treturn string.IsNullOrEmpty(s) ? string.Empty : ((s.Length > length) ? s.Substring(s.Length - length, length) : s);\r\n" +
            "}\r\n" +
            "public string Replace(string s, string oldValue, string newValue)\r\n" +
            "{\r\n" +
            "\treturn s.Replace(oldValue, newValue);\r\n" +
            "}\r\n" +
            "public string Substring(string s, int startIndex, int length)\r\n" +
            "{\r\n" +
            "\treturn s.Substring(startIndex, length);\r\n" +
            "}\r\n" +
            "public string ToLower(string s)\r\n" +
            "{\r\n" +
            "\treturn s.ToLower();\r\n" +
            "}\r\n" +
            "public string ToUpper(string s)\r\n" +
            "{\r\n" +
            "\treturn s.ToUpper();\r\n" +
            "}\r\n" +
            "public string Trim(string s, string trimString)\r\n" +
            "{\r\n" +
            "\treturn s.Trim(trimString.ToCharArray());\r\n" +
            "}\r\n" +
            "\r\n";

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
            WebBrowser.NavigateToString(helpContent);

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

                    var input =
                        new TextRange(TbxInput.Document.ContentStart, TbxInput.Document.ContentEnd).Text.Replace("\r", "");
                    var table = new Table(input, _engine.FieldDelimiter, filter);

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
            lines.Select(line =>
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
            _autoMode = CbxMode.SelectedIndex == 0;
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
            var lastSessionPattern = Settings.Default.LastSessionPattern;
            SetText(TbxPattern, string.IsNullOrEmpty(lastSessionPattern) ? DefaultLastSessionPattern : lastSessionPattern);

            var lastSessionInput = Settings.Default.LastSessionInput;
            SetText(TbxInput, string.IsNullOrEmpty(lastSessionInput) ? DefaultLastSessionInput : lastSessionInput);

            var lastSessionCode = Settings.Default.LastSessionCode;
            Editor.Text = string.IsNullOrEmpty(lastSessionCode) ? DefaultLastSessionCode : lastSessionCode;

            Application.Current.Exit += (sender, args) =>
            {
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

        private void TbcMain_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            BtnRun.Content = (TbcMain.SelectedIndex == 1) ? "Check (F5)" : "Run (F5)";
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

        #endregion Code Editor
    }
}
