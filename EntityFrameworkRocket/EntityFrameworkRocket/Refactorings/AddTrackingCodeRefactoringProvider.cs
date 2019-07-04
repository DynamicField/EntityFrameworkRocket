using System;
using System.Composition;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using EntityFrameworkRocket.Walkers;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CodeRefactorings;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.Rename;
using Microsoft.CodeAnalysis.Text;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;
namespace EntityFrameworkRocket.Refactorings
{
    [ExportCodeRefactoringProvider(LanguageNames.CSharp, Name = nameof(AddTrackingCodeRefactoringProvider)), Shared]
    public class AddTrackingCodeRefactoringProvider : CodeRefactoringProvider
    {
        // Dirty workaround to test this as the testing library does not support multiple refactorings
        internal bool DisableAsTracking { get; set; } = false;
        internal bool DisableAsNoTracking { get; set; } = false;
        public sealed override async Task ComputeRefactoringsAsync(CodeRefactoringContext context)
        {
            if (!context.Document.Project.HasAnyEntityFramework()) return;
            var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
            var semanticModel = await context.Document.GetSemanticModelAsync(context.CancellationToken).ConfigureAwait(false);

            var result = root.FindNode(context.Span).GetLinqQuery(semanticModel);
            if (result is null) return;

            Task<Document> ExecuteLocal(string methodName, CancellationToken token) => Execute(context.Document, result, methodName, token);
            if (!DisableAsNoTracking && (result.IsTracked ?? true))
            {
                CodeAction.Create("Add AsNoTracking()", t => ExecuteLocal( "AsNoTracking", t)).Register(context);
            }
            if (!DisableAsTracking && (!result.IsTracked ?? true))
            {
                CodeAction.Create("Add AsTracking()", t => ExecuteLocal("AsTracking", t)).Register(context);
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
