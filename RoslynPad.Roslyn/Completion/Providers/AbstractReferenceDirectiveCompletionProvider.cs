using System.Collections.Immutable;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Completion;
using Microsoft.CodeAnalysis.Editor.Implementation.IntelliSense.Completion.FileSystem;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Scripting.Hosting;
using Microsoft.CodeAnalysis.Text;

namespace RoslynPad.Roslyn.Completion.Providers
{
    internal abstract class AbstractReferenceDirectiveCompletionProvider : CompletionListProvider
    {
        protected abstract bool TryGetStringLiteralToken(SyntaxTree tree, int position, out SyntaxToken stringLiteral, CancellationToken cancellationToken);

        public override bool IsTriggerCharacter(SourceText text, int characterPosition, OptionSet options)
        {
            return PathCompletionUtilities.IsTriggerCharacter(text, characterPosition);
        }

        private TextSpan GetTextChangeSpan(SyntaxToken stringLiteral, int position)
        {
            return PathCompletionUtilities.GetTextChangeSpan(stringLiteral.ToString(), stringLiteral.SpanStart, position);
        }
        
        public override async Task ProduceCompletionListAsync(CompletionListContext context)
        {
            var document = context.Document;
            var position = context.Position;
            var cancellationToken = context.CancellationToken;

            var tree = await document.GetSyntaxTreeAsync(cancellationToken).ConfigureAwait(false);

            // first try to get the #r string literal token.  If we couldn't, then we're not in a #r
            // reference directive and we immediately bail.
            SyntaxToken stringLiteral;
            if (!TryGetStringLiteralToken(tree, position, out stringLiteral, cancellationToken))
            {
                return;
            }

            var textChangeSpan = GetTextChangeSpan(stringLiteral, position);

            var gacHelper = new GlobalAssemblyCacheCompletionHelper(this, textChangeSpan, ItemRules.Instance);
            var referenceResolver = document.Project.CompilationOptions.MetadataReferenceResolver;

            // TODO: https://github.com/dotnet/roslyn/issues/5263
            // Avoid dependency on a specific resolvers.
            // The search paths should be provided by specialized workspaces:
            // - InteractiveWorkspace for interactive window 
            // - ScriptWorkspace for loose .csx files (we don't have such workspace today)
            ImmutableArray<string> searchPaths;

            RuntimeMetadataReferenceResolver rtResolver;
            WorkspaceMetadataFileReferenceResolver workspaceResolver;

            if ((rtResolver = referenceResolver as RuntimeMetadataReferenceResolver) != null)
            {
                searchPaths = rtResolver.PathResolver.SearchPaths;
            }
            else if ((workspaceResolver = referenceResolver as WorkspaceMetadataFileReferenceResolver) != null)
            {
                searchPaths = workspaceResolver.PathResolver.SearchPaths;
            }
            else
            {
                return;
            }

            var fileSystemHelper = new FileSystemCompletionHelper(
                this, textChangeSpan,
                new CurrentWorkingDirectoryDiscoveryService(Directory.GetCurrentDirectory()),
                Microsoft.CodeAnalysis.Glyph.OpenFolder,
                Microsoft.CodeAnalysis.Glyph.Assembly, searchPaths, new[] { ".dll", ".exe" }, path => path.Contains(","), ItemRules.Instance);

            var pathThroughLastSlash = GetPathThroughLastSlash(stringLiteral, position);

            var documentPath = document.Project.IsSubmission ? null : document.FilePath;
            context.AddItems(gacHelper.GetItems(pathThroughLastSlash, documentPath));
            context.AddItems(fileSystemHelper.GetItems(pathThroughLastSlash, documentPath));
        }


        private static string GetPathThroughLastSlash(SyntaxToken stringLiteral, int position)
        {
            return PathCompletionUtilities.GetPathThroughLastSlash(stringLiteral.ToString(), stringLiteral.SpanStart, position);
        }

        private class CurrentWorkingDirectoryDiscoveryService : ICurrentWorkingDirectoryDiscoveryService
        {
            public CurrentWorkingDirectoryDiscoveryService(string workingDirectory)
            {
                WorkingDirectory = workingDirectory;
            }

            public string WorkingDirectory { get; }
        }

        private class ItemRules : Microsoft.CodeAnalysis.Completion.CompletionItemRules
        {
            public static readonly ItemRules Instance = new ItemRules();

            public override bool? IsCommitCharacter(Microsoft.CodeAnalysis.Completion.CompletionItem completionItem, char ch, string textTypedSoFar)
            {
                return PathCompletionUtilities.IsCommitcharacter(completionItem, ch, textTypedSoFar);
            }

            public override bool? IsFilterCharacter(Microsoft.CodeAnalysis.Completion.CompletionItem completionItem, char ch, string textTypedSoFar)
            {
                return PathCompletionUtilities.IsFilterCharacter(completionItem, ch, textTypedSoFar);
            }

            public override bool? SendEnterThroughToEditor(Microsoft.CodeAnalysis.Completion.CompletionItem completionItem, string textTypedSoFar, OptionSet options)
            {
                return PathCompletionUtilities.SendEnterThroughToEditor(completionItem, textTypedSoFar);
            }
        }
    }
}