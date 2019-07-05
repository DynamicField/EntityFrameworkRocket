using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeRefactorings;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Editing;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;
namespace EntityFrameworkRocket.Refactorings
{
    [ExportCodeRefactoringProvider(LanguageNames.CSharp, Name = nameof(AddTrackingCodeRefactoringProvider)), Shared]
    public class AddTrackingCodeRefactoringProvider : CodeRefactoringProvider
    {
        public const string AddAsNoTrackingTitle = "Add AsNoTracking()";
        public const string AddAsTrackingTitle = "Add AsTracking()";

        // Dirty workaround to test this as the testing library does not support multiple refactorings
        public sealed override async Task ComputeRefactoringsAsync(CodeRefactoringContext context)
        {
            if (!context.Document.Project.HasAnyEntityFramework()) return;
            var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
            var semanticModel = await context.Document.GetSemanticModelAsync(context.CancellationToken).ConfigureAwait(false);

            var result = root.FindNode(context.Span).GetLinqQuery(semanticModel);
            if (result is null) return;

            Task<Document> ExecuteLocal(string methodName, CancellationToken token) => Execute(context.Document, result, methodName, token);
            if (result.IsTracked ?? true)
            {
                CodeAction.Create(AddAsNoTrackingTitle, t => ExecuteLocal( "AsNoTracking", t), AddAsNoTrackingTitle).Register(context);
            }
            if (!result.IsTracked ?? true)
            {
                CodeAction.Create(AddAsTrackingTitle, t => ExecuteLocal("AsTracking", t), AddAsTrackingTitle).Register(context);
            }
        }

        private async Task<Document> Execute(Document document, LinqQuery query, string requestedMethodName, CancellationToken cancellationToken)
        {
            var editor = await DocumentEditor.CreateAsync(document, cancellationToken);
            var steps = query.Steps.Where(x => x.Name == "AsNoTracking" || x.Name == "AsTracking").ToList();
            if (steps.Any())
            {
                var s = steps.Last();
                editor.ReplaceNode(s.Invocation.Expression, ((MemberAccessExpressionSyntax)s.Invocation.Expression).WithName(IdentifierName(requestedMethodName)));
            }
            else
            {
                editor.ReplaceNode(query.SourceCollection,
                                   InvocationExpression(
                                       MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression,
                                                                 query.SourceCollection, IdentifierName(requestedMethodName))));
                // context.Things.ToList() -> context.Things.AsNoTracking().ToList();
            }
            return editor.GetChangedDocument();
        }
    }
}
