using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Linq.Expressions;
using EntityFrameworkRocket.Walkers;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace EntityFrameworkRocket
{
    internal static partial class RoslynExtensions
    {
        public static bool IsNotMapped(this IPropertySymbol property)
        {
            return property.GetAttributes().Any(a => a.AttributeClass.Name == EntityFrameworkConstants.NotMappedAttribute);
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

        public static bool HasEntityFrameworkCore(this IEnumerable<MetadataReference> refs)
        {
            return refs.Any(p => p.Display.EndsWith(EntityFrameworkConstants.EntityFrameworkCoreDll));
        }
        public static bool HasEntityFrameworkClassic(this IEnumerable<MetadataReference> refs)
        {
            return refs.Any(p => p.Display.EndsWith(EntityFrameworkConstants.EntityFrameworkClassicDll));
        }
        public static IEnumerable<ITypeSymbol> GetBaseTypesAndThis(this ITypeSymbol type)
        {
            var current = type;
            while (current != null)
            {
                yield return current;
                current = current.BaseType;
            }
        }

        public static IEnumerable<INamedTypeSymbol> GetBaseTypesAndThis(this INamedTypeSymbol namedType) 
            => GetBaseTypesAndThis((ITypeSymbol)namedType).Cast<INamedTypeSymbol>();

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
        public static string ToPortableString(this TextSpan span)
        {
            return span.Start + "+" + span.Length;
        }

        public static TextSpan TextSpanFromPortableString(this string span)
        {
            var parts = span.Split('+');
            return new TextSpan(int.Parse(parts[0]), int.Parse(parts[1]));
        }
        public static bool IsAsync(this SyntaxToken token)
        {
            return token.Text.EndsWith("Async");
        }
        public static bool IsAsync(this SimpleNameSyntax nameSyntax)
        {
            return nameSyntax.Identifier.IsAsync();
        }
        public static bool IsAsync(this MemberAccessExpressionSyntax memberAccess)
        {
            return memberAccess.Name.IsAsync();
        }
    }
}
