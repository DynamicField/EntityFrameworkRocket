using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace EntityFrameworkRocket.Analyzers
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class UseFindAnalyzer : DiagnosticAnalyzer
    {
        internal const string DiagnosticId = "EFX0002";
        private const string Title = "Use Find instead of a LINQ query";
        private const string MessageFormat = "This query can be simplified with {0}.";
        private const string Category = "LINQ";

        private static readonly DiagnosticDescriptor Rule = new DiagnosticDescriptor(DiagnosticId, Title, MessageFormat, Category, DiagnosticSeverity.Info, true);

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

        public override void Initialize(AnalysisContext context)
        {
            context.RegisterSyntaxNodeAction(Execute, SyntaxKind.InvocationExpression);
        }

        internal static readonly IEnumerable<string> CompatibleMethods =
        new[]{
            nameof(Enumerable.FirstOrDefault),
            nameof(Enumerable.First),
            nameof(Enumerable.LastOrDefault),
            nameof(Enumerable.Last),
            nameof(Enumerable.SingleOrDefault),
            nameof(Enumerable.Single)
        }.SelectMany(s => new[] { s, s + "Async" });

        public const string OppositeExpression = nameof(OppositeExpression);

        private void Execute(SyntaxNodeAnalysisContext context)
        {
            var query = context.GetLinqQuery();
            var method = query?.Steps.FirstOrDefault(x => CompatibleMethods.Contains(x.Name));
            if (method is null) return;

            var sourceType = context.SemanticModel.GetTypeInfo(method.Source, context.CancellationToken).Type;
            // If it is not a DbSet, it must be an IQueryable<T> returned from another method (ex: AsNoTracking() returns a IQueryable<T>)
            if (sourceType?.Name != EntityFrameworkConstants.DbSet) return; 

            // At this point we should have something like context.Things.FirstOrDefault() with Things being a DbSet.
            if (method.Invocation.ArgumentList.Arguments.FirstOrDefault()?.Expression is SimpleLambdaExpressionSyntax lambda)
            {
                var parameterSymbol =
                    context.SemanticModel.GetDeclaredSymbol(lambda.Parameter, context.CancellationToken);
                var idProperty = parameterSymbol?.Type.GetMembers().OfType<IPropertySymbol>().FirstOrDefault(x => x.IsId());
                if (idProperty is null) return; // No id = nope.

                if (IsValidLambda(lambda, out var binaryExpression))
                {
                    var idExpression =
                        GetExpressionUsingIdProperty(binaryExpression, context.SemanticModel, idProperty);
                    if (idExpression is null) return;

                    var oppositeExpression = GetOppositeExpression(idExpression, binaryExpression);
                    
                    // Here we check if the lambda parameter has been used in the other operand.
                    // If it's the case, it cannot be simplified.
                    if (oppositeExpression.DescendantNodes()
                        .OfType<IdentifierNameSyntax>().Any(i => i.Identifier.Text == parameterSymbol.Name)) return;

                    var findName = method.Name.EndsWith("Async")
                        ? EntityFrameworkConstants.FindAsync
                        : EntityFrameworkConstants.Find;
                    var properties = new Dictionary<string, string>
                    {
                        { OppositeExpression, oppositeExpression.Span.ToPortableString() }
                    }.ToImmutableDictionary();
                    context.ReportDiagnostic(Diagnostic.Create(Rule, method.Invocation.GetLocation(), properties, findName));
                }
            }
        }

        private static ExpressionSyntax GetOppositeExpression(MemberAccessExpressionSyntax idExpression, BinaryExpressionSyntax binaryExpression)
        {
            return idExpression == binaryExpression.Left
                ? binaryExpression.Right
                : binaryExpression.Left;
        }

        private static bool IsValidLambda(AnonymousFunctionExpressionSyntax lambda, out BinaryExpressionSyntax binaryExpressionSyntax)
        {
            binaryExpressionSyntax = null;
            return lambda.Body is BinaryExpressionSyntax binaryExpression && (binaryExpressionSyntax = binaryExpression) is object &&
                   binaryExpressionSyntax.OperatorToken.IsKind(SyntaxKind.EqualsEqualsToken) &&
                   !binaryExpressionSyntax.Left.IsEquivalentTo(binaryExpressionSyntax.Right);
        }

        private MemberAccessExpressionSyntax GetExpressionUsingIdProperty(BinaryExpressionSyntax syntax,
            SemanticModel semanticModel,
            ISymbol idProperty)
        {
            bool IsValid(ExpressionSyntax node, out MemberAccessExpressionSyntax expression)
            {
                if (node is MemberAccessExpressionSyntax m)
                {
                    expression = m;
                    return semanticModel.GetSymbolInfo(m).Symbol?.Equals(idProperty) ?? false;
                }
                expression = null;
                return false;
            }
            if (IsValid(syntax.Left, out var left)) return left;
            return IsValid(syntax.Right, out var right) ? right : null;
        }
    }
}
