using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Markup;
using System.Windows.Media;
using Microsoft.CodeAnalysis.CodeActions;
using RoslynPad.Roslyn.CodeActions;
using RoslynPad.Roslyn.Completion;

namespace TableTweaker.Formatting
{
    internal sealed class CodeActionToGlyphConverter : MarkupExtension, IValueConverter
    {
        public override object ProvideValue(IServiceProvider serviceProvider)
        {
            return this;
        }

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var glyphNumber = ((CodeAction)value).GetGlyph();
            if (glyphNumber == null) return null;
            return Application.Current.TryFindResource((Glyph)glyphNumber.Value) as ImageSource;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }
}