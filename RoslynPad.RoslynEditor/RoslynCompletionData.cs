﻿using System;
using System.Windows.Media;
using ICSharpCode.AvalonEdit.Document;
using ICSharpCode.AvalonEdit.Editing;
using RoslynPad.Editor;
using RoslynPad.Roslyn;
using RoslynPad.Roslyn.Completion;

namespace RoslynPad.RoslynEditor
{
    internal sealed class RoslynCompletionData : ICompletionDataEx
    {
        private readonly CompletionItem _item;
        private object _description;

        public RoslynCompletionData(CompletionItem item)
        {
            _item = item;
            Text = item.DisplayText;
            Content = item.DisplayText;
            if (item.Glyph != null)
            {
                Image = item.Glyph.Value.ToImageSource();
            }
        }

        public void Complete(TextArea textArea, ISegment completionSegment, EventArgs e)
        {
            var change = _item.Rules.GetTextChange(_item);
            var text = change?.NewText ?? _item.DisplayText; // workaround for keywords
            textArea.Document.Replace(completionSegment, text);
        }

        public ImageSource Image { get; }

        public string Text { get; }

        public object Content { get; }
        
        public object Description
        {
            get
            {
                if (_description == null)
                {
                    _description = _item.GetDescriptionAsync().Result.ToTextBlock();
                }
                return _description;
            }
        }

        // ReSharper disable once UnusedAutoPropertyAccessor.Local
        public double Priority { get; private set; }

        public bool IsSelected => _item.Preselect;

        public string SortText => _item.SortText;
    }
}