using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeRefactorings;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace EntityFrameworkRocket
{
    internal static class RoslynExtensions
    {
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
        public static void Register(this CodeAction action, CodeRefactoringContext context) =>
            context.RegisterRefactoring(action);

        public static ExpressionSyntax RemoveCall(this InvocationExpressionSyntax node)
        {
            var n = node.Expression;
            if (n is MemberAccessExpressionSyntax memberAccess) n = memberAccess.Expression;
            return n;
        }
    }
}
