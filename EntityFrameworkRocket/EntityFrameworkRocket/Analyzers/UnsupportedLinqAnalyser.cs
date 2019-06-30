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
    public class UnsupportedLinqAnalyser : DiagnosticAnalyzer
    {
        private const string DiagnosticId = "EFX0001";
        private static readonly string Title = "Unsupported LINQ expression";
        private static readonly string MessageFormat 
            = "This version of Entity Framework does not support {0}, this expression may throw an exception at runtime.";
        private const string Category = "LINQ";

        private static readonly DiagnosticDescriptor Rule = new DiagnosticDescriptor(DiagnosticId, Title, MessageFormat,
            Category, DiagnosticSeverity.Warning, true);

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

        public override void Initialize(AnalysisContext context)
        {
            context.RegisterSyntaxNodeAction(AnalyzeNode, SyntaxKind.InvocationExpression);
        }

        private static void AnalyzeNode(SyntaxNodeAnalysisContext context)
        {
            var query = context.Node.GetLinqQuery(context.SemanticModel);
            if (query is null) return;
            if (!query.Expression.IsEquivalentTo(context.Node)) return;
            var sourceCollectionType = context.SemanticModel.GetTypeInfo(query.SourceCollection).Type;
            // We ensure that it comes from a DbSet to avoid conflict with other libraries that may use IQueryable.
            if (sourceCollectionType?.Name != "DbSet") return;
            foreach (var step in query.Steps)
            {
                if (step.Symbol.ReceiverType.Name != nameof(IQueryable)) return; // If it has been used as en IEnumerable, it is executed client side.
                switch (step.Name)
                {
                    case nameof(Enumerable.Select):
                    case nameof(Enumerable.Where):
                    case nameof(Enumerable.SelectMany):
                    case nameof(Enumerable.SkipWhile):
                    case nameof(Enumerable.TakeWhile):
                        // Checks if the Func<T, int> has been used.
                        if (step.Symbol.Parameters.FirstOrDefault()?.Type is INamedTypeSymbol lambda &&
                            lambda.GetUnderlyingExpressionType().TypeArguments.ElementAtOrDefault(1)?.Name == nameof(Int32))
                        {
                            // Take i from (x, i)
                            var locationTarget =
                                (step.Invocation.ArgumentList.Arguments.FirstOrDefault()?.Expression as
                                    ParenthesizedLambdaExpressionSyntax)?.ParameterList.Parameters.ElementAtOrDefault(1) as SyntaxNode ?? step.Invocation;
                            var diagnostic = Diagnostic.Create(Rule, locationTarget.GetLocation(),
                                $"using the second index parameter in {step.Name}");
                            context.ReportDiagnostic(diagnostic);
                        }
                        break;
                    default:
                        continue;
                }
            }
        }
    }
}
