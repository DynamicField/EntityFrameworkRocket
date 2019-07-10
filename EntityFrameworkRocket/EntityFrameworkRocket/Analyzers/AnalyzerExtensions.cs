using System.Linq;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace EntityFrameworkRocket.Analyzers
{
    internal static class AnalyzerExtensions
    {
        /// <summary>
        /// Gets the nearest LINQ query that is the current node.
        /// </summary>
        /// <returns>The LINQ query.</returns>
        public static LinqQuery GetLinqQuery(this SyntaxNodeAnalysisContext context, bool keepNonLinqQueries = false)
        {
            var query = context.Node.GetLinqQuery(context.SemanticModel, keepNonLinqQueries);
            if (query is null) return null;
            switch (query.Expression)
            {
                case AwaitExpressionSyntax _:
                case ParenthesizedExpressionSyntax _:
                    if (!query.Expression.IsEquivalentTo(context.Node)
                        && !(query.Expression.DescendantNodes().FirstOrDefault()?.IsEquivalentTo(context.Node) ?? false))
                    {
                        return null;
                    }
                    goto default;
                case InvocationExpressionSyntax _:
                    if (!query.Expression.IsEquivalentTo(context.Node)) return null;
                    break;
                default: return query;
            }
            if (!query.Expression.IsEquivalentTo(context.Node) &&
                (!(query.Expression is AwaitExpressionSyntax awaited) ||
                 !awaited.Expression.IsEquivalentTo(context.Node)))
            {
                return null;
            }
            return query;
        }
    }
}