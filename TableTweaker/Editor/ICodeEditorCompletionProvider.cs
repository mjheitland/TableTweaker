using System.Threading.Tasks;

namespace TableTweaker.Editor
{
    public interface ICodeEditorCompletionProvider
    {
        Task<CompletionResult> GetCompletionData(int position, char? triggerChar);
    }
}