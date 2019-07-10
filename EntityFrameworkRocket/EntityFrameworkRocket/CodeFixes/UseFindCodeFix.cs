using System;
using System.Composition;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using EntityFrameworkRocket.Analyzers;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.Rename;
using Microsoft.CodeAnalysis.Text;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;
namespace EntityFrameworkRocket.CodeFixes
{
    [ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(UseFindCodeFix)), Shared]
    public class UseFindCodeFix : CodeFixProvider
    {
        public const string DiagnosticId = UseFindAnalyzer.DiagnosticId;

        public sealed override ImmutableArray<string> FixableDiagnosticIds => ImmutableArray.Create(DiagnosticId);

        public sealed override FixAllProvider GetFixAllProvider() => WellKnownFixAllProviders.BatchFixer;

        public sealed override async Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken);

            if (!(root.FindNode(context.Span) is InvocationExpressionSyntax node)) return;
            if (!(node.Expression is MemberAccessExpressionSyntax memberAccess)) return;

            var methodName = GetFindMethodName(memberAccess);
            var title = $"Replace with {methodName}";
            foreach (var diagnostic in context.Diagnostics)
            {
                var idValue = (ExpressionSyntax)root.FindNode(diagnostic.Properties[UseFindAnalyzer.OppositeExpression].TextSpanFromPortableString());

                var action = CodeAction.Create(title, t => Execute(context.Document, node, idValue, memberAccess, methodName, t), title);
                context.RegisterCodeFix(action, diagnostic);
            }
        }


        private static async Task<Document> Execute(Document document,
            InvocationExpressionSyntax node,
            ExpressionSyntax idValue,
            MemberAccessExpressionSyntax memberAccess,
            string methodName,
            CancellationToken cancellationToken)
        {
            var editor = await DocumentEditor.CreateAsync(document, cancellationToken);

            ExpressionSyntax invocation = node // First(x => x.Id == 5)
                .WithExpression(memberAccess.WithName(IdentifierName(methodName))) // Find(x => x.Id == 5)
                .WithArgumentList(node.ArgumentList.WithArguments(
                    SeparatedList(
                        new[] { Argument(idValue) })));

            editor.ReplaceNode(node, invocation);
            return editor.GetChangedDocument();
        }

        private static string GetFindMethodName(MemberAccessExpressionSyntax memberAccess)
        {
            var isAsync = memberAccess.IsAsync();
            return isAsync
                ? EntityFrameworkConstants.FindAsync
                : EntityFrameworkConstants.Find;
        }
    }
}
