using ICSharpCode.AvalonEdit.CodeCompletion;

namespace TableTweaker.Editor
{
    public interface ICompletionDataEx : ICompletionData
    {
        bool IsSelected { get; }

        string SortText { get; }
    }
}