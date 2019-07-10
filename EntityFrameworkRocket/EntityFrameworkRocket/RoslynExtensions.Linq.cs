using System;
using System.Collections;
using System.Linq;
using System.Linq.Expressions;
using EntityFrameworkRocket.Walkers;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace EntityFrameworkRocket
{
    internal static partial class RoslynExtensions
    {
        public static bool IsDbContext(this ExpressionSyntax expression, SemanticModel semanticModel)
        {
            return expression.CheckInheritors(t => t.Name == EntityFrameworkConstants.DbContext, semanticModel);
        }

        public static bool IsDbSet(this ExpressionSyntax expression, SemanticModel semanticModel)
        {
            return expression.CheckInheritors(t => t.Name == EntityFrameworkConstants.DbSet, semanticModel);
        }

        public static bool IsQueryable(this ExpressionSyntax expression, SemanticModel semanticModel)
        {
            var symbol = semanticModel.GetTypeInfo(expression).Type ??
                         semanticModel.GetTypeInfo(expression).ConvertedType;
            return symbol != null && (symbol.Name == nameof(IQueryable) ||
                                      symbol.AllInterfaces.Any(x => x.Name == nameof(IQueryable)));
        }

        public static LinqQuery GetLinqQuery(this SyntaxNode node, SemanticModel semanticModel,
            bool keepNonLinqQueries = false)
        {
            var source = node.GetQueryableExpression(semanticModel);
            if (source is null) return null;
            var type = semanticModel.GetTypeInfo(source).ConvertedType;
            // If the source collection has not been converted to LINQ's generic interface methods, it is not a linq query.
            if (!keepNonLinqQueries &&
                type != null &&
                type.Name != nameof(IQueryable) &&
                type.Name != nameof(IEnumerable) &&
                type.Name != nameof(ILookup<object, object>)) return null;
            var completeExpression = source?.Ancestors().OfType<ExpressionSyntax>().TakeWhile(e =>
            {
                switch (e)
                {
                    case AwaitExpressionSyntax _:
                    case ParenthesizedExpressionSyntax _:
                    case InvocationExpressionSyntax _:
                        return true;
                    case MemberAccessExpressionSyntax _:
                        return e.Parent is InvocationExpressionSyntax;
                    default:
                        return false;
                }
            }).LastOrDefault();
            if (completeExpression is null) return null;
            return new LinqQuerySyntaxWalker(semanticModel).VisitQuery(completeExpression, source);
        }

        public static ExpressionSyntax GetQueryableExpression(this SyntaxNode node,
            SemanticModel semanticModel)
        {
            return ((ExpressionSyntax) node.FirstAncestorOrSelf<MemberAccessExpressionSyntax>() ??
                    node.FirstAncestorOrSelf<InvocationExpressionSyntax>())?
                .DescendantNodesAndSelf()
                .OfType<ExpressionSyntax>()
                .Where(x => x.Parent is MemberAccessExpressionSyntax &&
                            (!x.TypeEquals(x.Parent, semanticModel) ??
                             true)) // If the parent has the exact same type, use it instead.
                .LastOrDefault(m => m.IsQueryable(semanticModel));
        }

        public static INamedTypeSymbol GetUnderlyingExpressionType(this INamedTypeSymbol symbol)
        {
            if (symbol.Name == nameof(Expression<Action>) && symbol.TypeArguments.Any())
            {
                return (symbol.TypeArguments.FirstOrDefault() as INamedTypeSymbol) ?? symbol;
            }

            return symbol;
        }
    }
}