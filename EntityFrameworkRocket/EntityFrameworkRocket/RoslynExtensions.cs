using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using EntityFrameworkRocket.Walkers;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace EntityFrameworkRocket
{
    internal static partial class RoslynExtensions
    {
        public static bool IsNotMapped(this IPropertySymbol property)
        {
            return property.GetAttributes().Any(a => a.AttributeClass.Name == "NotMappedAttribute");
        }
        /// <summary>
        /// Checks whether or not a property is, by Entity Framework conventions, a primary key.
        /// </summary>
        /// <param name="property">The property</param>
        /// <returns></returns>
        public static bool IsId(this IPropertySymbol property)
        {
            return property.Name == "Id" || property.Name == "ID" ||
                   property.ContainingType != null && property.Name.IdCheck(property.ContainingType.Name);
        }
        public static bool IsNavigationPropertyId(this IPropertySymbol property, IPropertySymbol navigationProperty)
        {
            return property.Name.IdCheck(navigationProperty.Name);
        }
        private static bool IdCheck(this string propertyName, string composite = "", Predicate<string> predicate = null)
        {
            predicate = predicate ?? (s => s == propertyName);
            return predicate(composite + "Id") || predicate(composite + "ID");
        }

        public static bool IsDbContext(this ExpressionSyntax expression, SemanticModel semanticModel)
        {
            return expression.CheckInheritors(t => t.Name == "DbContext", semanticModel);
        }
        public static bool IsDbSet(this ExpressionSyntax expression, SemanticModel semanticModel)
        {
            return expression.CheckInheritors(t => t.Name == "DbSet", semanticModel);
        }

        public static bool IsQueryable(this ExpressionSyntax expression, SemanticModel semanticModel)
        {
            var symbol = semanticModel.GetTypeInfo(expression).Type ?? semanticModel.GetTypeInfo(expression).ConvertedType;
            return symbol != null && (symbol.Name == nameof(IQueryable) || symbol.AllInterfaces.Any(x => x.Name == nameof(IQueryable)));
        }
        public static LinqQuery GetLinqQuery(this SyntaxNode node, SemanticModel semanticModel)
        {
            var source = node.GetQueryableExpression(semanticModel);
            if (source is null) return null;
            var type = semanticModel.GetTypeInfo(source).ConvertedType;
            // If the source collection has not been converted to LINQ's generic interface methods, it is not a linq query.
            if (type != null && type.Name != nameof(IQueryable) && type.Name != nameof(IEnumerable) && type.Name != nameof(ILookup<object, object>)) return null;
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
            return ((ExpressionSyntax)node.FirstAncestorOrSelf<MemberAccessExpressionSyntax>() ?? node.FirstAncestorOrSelf<InvocationExpressionSyntax>())?
                .DescendantNodesAndSelf()
                .OfType<ExpressionSyntax>()
                .Where(x => x.Parent is MemberAccessExpressionSyntax && (!x.TypeEquals(x.Parent, semanticModel) ?? true)) // If the parent has the exact same type, use it instead.
                .LastOrDefault(m => m.IsQueryable(semanticModel));
        }
        public static INamedTypeSymbol GetUnderlyingExpressionType(this INamedTypeSymbol symbol)
        {
            if (symbol.Name == "Expression" && symbol.TypeArguments.Any())
            {
                return (symbol.TypeArguments.FirstOrDefault() as INamedTypeSymbol) ?? symbol;
            }

            return symbol;
        }

        public static bool HasEntityFrameworkCore(this IEnumerable<MetadataReference> refs)
        {
            return refs.Any(p => p.Display.EndsWith("Microsoft.EntityFrameworkCore.dll"));
        }
        public static bool HasEntityFrameworkClassic(this IEnumerable<MetadataReference> refs)
        {
            return refs.Any(p => p.Display.EndsWith("EntityFramework.dll"));
        }
        public static IEnumerable<INamedTypeSymbol> GetBaseTypesAndThis(this INamedTypeSymbol namedType)
        {
            var current = namedType;
            while (current != null)
            {
                yield return current;
                current = current.BaseType;
            }
        }
        public static bool CheckInheritors(this INamedTypeSymbol type, Func<INamedTypeSymbol, bool> predicate)
        {
            return type.GetBaseTypesAndThis().Any(predicate);
        }
        public static bool CheckInheritors(this ExpressionSyntax expression, Func<INamedTypeSymbol, bool> predicate, SemanticModel semanticModel)
        {
            var type = semanticModel.GetTypeInfo(expression);
            return ((type.Type ?? type.ConvertedType) as INamedTypeSymbol).CheckInheritors(predicate);
        }

        public static bool? TypeEquals(this SyntaxNode expression, SyntaxNode other, SemanticModel semanticModel)
        {
            if (other is null || expression is null) return null;
            var expressionType = semanticModel.GetTypeInfo(expression).Type;
            var otherType = semanticModel.GetTypeInfo(other).Type;
            if (otherType is null || expressionType is null) return null;
            return otherType.Equals(expressionType);
        }

        public static ExpressionSyntax RemoveCall(this InvocationExpressionSyntax node)
        {
            var n = node.Expression;
            if (n is MemberAccessExpressionSyntax memberAccess) n = memberAccess.Expression;
            return n;
        }
    }
}
