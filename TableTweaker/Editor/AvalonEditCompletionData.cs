﻿using System;
using System.Windows.Media;
using ICSharpCode.AvalonEdit.Document;
using ICSharpCode.AvalonEdit.Editing;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Completion;

namespace TableTweaker.Editor
{
    public class AvalonEditCompletionData : ICompletionDataEx
    {
        private readonly CompletionItem _item;
        private object _description;

        public AvalonEditCompletionData(CompletionItem item)
        {
            _item = item;
            Text = item.DisplayText;
            Content = item.DisplayText;
            // Image = item.Glyph;
        }

        public void Complete(TextArea textArea, ISegment completionSegment, EventArgs e)
        {
            var change = _item.CompletionProvider.GetTextChange(_item);
            textArea.Document.Replace(completionSegment, change.NewText);
        }

        // ReSharper disable once UnusedAutoPropertyAccessor.Local
        public ImageSource Image { get; private set; }

        public string Text { get; }

        public object Content { get; }

        public object Description => _description ?? 
                                     (_description = _item.GetDescriptionAsync().Result.ToDisplayString());

        // ReSharper disable once UnusedAutoPropertyAccessor.Local
        public double Priority { get; private set; }

        public bool IsSelected => _item.Preselect;

        public string SortText => _item.SortText;
    }
}