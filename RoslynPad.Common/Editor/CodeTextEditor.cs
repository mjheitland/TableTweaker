﻿using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using ICSharpCode.AvalonEdit;
using ICSharpCode.AvalonEdit.CodeCompletion;
using ICSharpCode.AvalonEdit.Document;
using ICSharpCode.AvalonEdit.Highlighting;
using ICSharpCode.AvalonEdit.Search;

namespace RoslynPad.Editor
{
    public delegate void ToolTipRequestEventHandler(object sender, ToolTipRequestEventArgs args);

    public class CodeTextEditor : TextEditor
    {
        private CompletionWindow _completionWindow;
        private OverloadInsightWindow _insightWindow;

        public CodeTextEditor()
        {
            MouseHover += OnMouseHover;
            MouseHoverStopped += OnMouseHoverStopped;
            TextArea.TextView.VisualLinesChanged += OnVisualLinesChanged;
            TextArea.TextEntering += OnTextEntering;
            TextArea.TextEntered += OnTextEntered;
            ShowLineNumbers = true;
            ToolTipService.SetInitialShowDelay(this, 0);
            SearchPanel.Install(this);
        }

        public static readonly DependencyProperty CompletionBackgroundProperty = DependencyProperty.Register(
            "CompletionBackground", typeof(Brush), typeof(CodeTextEditor), new FrameworkPropertyMetadata(CreateDefaultCompletionBackground()));

        private ToolTip _toolTip;

        private static SolidColorBrush CreateDefaultCompletionBackground()
        {
            var defaultCompletionBackground = new SolidColorBrush(Color.FromRgb(240, 240, 240));
            defaultCompletionBackground.Freeze();
            return defaultCompletionBackground;
        }

        public Brush CompletionBackground
        {
            get { return (Brush)GetValue(CompletionBackgroundProperty); }
            set { SetValue(CompletionBackgroundProperty, value); }
        }

        protected override void OnKeyDown(KeyEventArgs e)
        {
            base.OnKeyDown(e);

            if (e.Key == Key.Space && e.KeyboardDevice.Modifiers.HasFlag(ModifierKeys.Control))
            {
                e.Handled = true;
                var mode = e.KeyboardDevice.Modifiers.HasFlag(ModifierKeys.Shift)
                    ? TriggerMode.SignatureHelp
                    : TriggerMode.Completion;
                // ReSharper disable once UnusedVariable
                var task = ShowCompletion(mode);
            }
        }

        private enum TriggerMode
        {
            Text,
            Completion,
            SignatureHelp
        }

        public static readonly RoutedEvent ToolTipRequestEvent = EventManager.RegisterRoutedEvent("ToolTipRequest",
            RoutingStrategy.Bubble, typeof(ToolTipRequestEventHandler), typeof(CodeTextEditor));

        public event ToolTipRequestEventHandler ToolTipRequest
        {
            add { AddHandler(ToolTipRequestEvent, value); }
            remove { RemoveHandler(ToolTipRequestEvent, value); }
        }

        private void OnVisualLinesChanged(object sender, EventArgs e)
        {
            if (_toolTip != null)
            {
                _toolTip.IsOpen = false;
            }
        }

        private void OnMouseHoverStopped(object sender, MouseEventArgs e)
        {
            if (_toolTip != null)
            {
                _toolTip.IsOpen = false;
                e.Handled = true;
            }
        }

        private void OnMouseHover(object sender, MouseEventArgs e)
        {
            TextViewPosition? position;
            try
            {
                position = TextArea.TextView.GetPositionFloor(e.GetPosition(TextArea.TextView) + TextArea.TextView.ScrollOffset);
            }
            catch (ArgumentOutOfRangeException)
            {
                // TODO: check why this happens
                e.Handled = true;
                return;
            }
            var args = new ToolTipRequestEventArgs { InDocument = position.HasValue };
            if (!position.HasValue || position.Value.Location.IsEmpty)
            {
                return;
            }

            args.LogicalPosition = position.Value.Location;

            RaiseEvent(args);

            if (args.ContentToShow == null) return;

            if (_toolTip == null)
            {
                _toolTip = new ToolTip { MaxWidth = 300 };
                _toolTip.Closed += ToolTipClosed;
                ToolTipService.SetInitialShowDelay(_toolTip, 0);
            }
            _toolTip.PlacementTarget = this; // required for property inheritance

            var stringContent = args.ContentToShow as string;
            if (stringContent != null)
            {
                _toolTip.Content = new TextBlock
                {
                    Text = stringContent,
                    TextWrapping = TextWrapping.Wrap
                };
            }
            else
            {
                _toolTip.Content = args.ContentToShow;
            }

            e.Handled = true;
            _toolTip.IsOpen = true;
        }

        private void ToolTipClosed(object sender, EventArgs e)
        {
            _toolTip = null;
        }

        #region Open & Save File

        public void OpenFile(string fileName)
        {
            if (!File.Exists(fileName))
            {
                throw new FileNotFoundException(fileName);
            }

            _completionWindow?.Close();
            _insightWindow?.Close();

            Load(fileName);
            Document.FileName = fileName;

            SyntaxHighlighting = HighlightingManager.Instance.GetDefinitionByExtension(Path.GetExtension(fileName));
        }

        public bool SaveFile()
        {
            if (string.IsNullOrEmpty(Document.FileName))
            {
                return false;
            }

            Save(Document.FileName);
            return true;
        }

        #endregion

        #region Code Completion

        public ICodeEditorCompletionProvider CompletionProvider { get; set; }

        private void OnTextEntered(object sender, TextCompositionEventArgs textCompositionEventArgs)
        {
            // ReSharper disable once UnusedVariable
            var task = ShowCompletion(TriggerMode.Text);
        }

        private async Task ShowCompletion(TriggerMode triggerMode)
        {
            if (CompletionProvider == null)
            {
                return;
            }

            int offset;
            GetCompletionDocument(out offset);
            var completionChar = triggerMode == TriggerMode.Text ? Document.GetCharAt(offset - 1) : (char?)null;
            var results = await CompletionProvider.GetCompletionData(offset, completionChar,
                        triggerMode == TriggerMode.SignatureHelp).ConfigureAwait(true);
            if (results.OverloadProvider != null)
            {
                results.OverloadProvider.Refresh();

                if (_insightWindow != null && _insightWindow.IsVisible)
                {
                    _insightWindow.Provider = results.OverloadProvider;
                }
                else
                {
                    _insightWindow = new OverloadInsightWindow(TextArea)
                    {
                        Provider = results.OverloadProvider,
                        Background = CompletionBackground,
                        Style = TryFindResource(typeof(InsightWindow)) as Style
                    };
                    _insightWindow.Show();
                    _insightWindow.Closed += (o, args) => _insightWindow = null;
                }
                return;
            }

            if (_completionWindow == null && results.CompletionData?.Any() == true)
            {
                _insightWindow?.Close();

                // Open code completion after the user has pressed dot:
                _completionWindow = new CompletionWindow(TextArea)
                {
                    MinWidth = 200,
                    Background = CompletionBackground,
                    CloseWhenCaretAtBeginning = triggerMode == TriggerMode.Completion
                };
                if (completionChar != null && char.IsLetterOrDigit(completionChar.Value))
                {
                    _completionWindow.StartOffset -= 1;
                }

                var data = _completionWindow.CompletionList.CompletionData;
                ICompletionDataEx selected = null;
                foreach (var completion in results.CompletionData) //.OrderBy(item => item.SortText))
                {
                    if (completion.IsSelected)
                    {
                        selected = completion;
                    }
                    data.Add(completion);
                }
                if (selected != null)
                {
                    _completionWindow.CompletionList.SelectedItem = selected;
                }
                _completionWindow.Show();
                _completionWindow.Closed += (o, args) => { _completionWindow = null; };
            }
        }

        private void OnTextEntering(object sender, TextCompositionEventArgs args)
        {
            if (args.Text.Length > 0 && _completionWindow != null)
            {
                if (!char.IsLetterOrDigit(args.Text[0]))
                {
                    // Whenever a non-letter is typed while the completion window is open,
                    // insert the currently selected element.
                    _completionWindow.CompletionList.RequestInsertion(args);
                }
            }
            // Do not set e.Handled=true.
            // We still want to insert the character that was typed.
        }

        /// <summary>
        /// Gets the document used for code completion, can be overridden to provide a custom document
        /// </summary>
        /// <param name="offset"></param>
        /// <returns>The document of this text editor.</returns>
        protected virtual IDocument GetCompletionDocument(out int offset)
        {
            offset = CaretOffset;
            return Document;
        }

        #endregion
    }
}
