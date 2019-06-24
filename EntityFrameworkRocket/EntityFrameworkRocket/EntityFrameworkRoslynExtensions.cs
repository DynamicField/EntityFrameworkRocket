using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;

namespace EntityFrameworkRocket
{
    internal static class EntityFrameworkRoslynExtensions
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
    }
}
