using System;
using System.Composition;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CodeRefactorings;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.Rename;
using Microsoft.CodeAnalysis.Text;
using SF = Microsoft.CodeAnalysis.CSharp.SyntaxFactory;
namespace EntityFrameworkRocket.Refactorings
{
    [ExportCodeRefactoringProvider(LanguageNames.CSharp, Name = nameof(AddNavigationPropertyKeyCodeRefactoringProvider)), Shared]
    internal class AddNavigationPropertyKeyCodeRefactoringProvider : CodeRefactoringProvider
    {
        public sealed override async Task ComputeRefactoringsAsync(CodeRefactoringContext context)
        {
            var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
            var semanticModel = await context.Document.GetSemanticModelAsync(context.CancellationToken).ConfigureAwait(false);

            var navigationProperty = root.FindNode(context.Span).FirstAncestorOrSelf<PropertyDeclarationSyntax>();
            var modelClass = navigationProperty?.FirstAncestorOrSelf<ClassDeclarationSyntax>();
            if (navigationProperty is null || navigationProperty.Type is PredefinedTypeSyntax) return;

            var propertyType = (ITypeSymbol)semanticModel.GetSymbolInfo(navigationProperty?.Type).Symbol;
            var idProperty = propertyType?.GetMembers().OfType<IPropertySymbol>().FirstOrDefault(p => p.IsId());
            if (idProperty is null)
            {
                return;
            }

            var propertySymbol = semanticModel.GetDeclaredSymbol(navigationProperty, context.CancellationToken);
            var modelType = semanticModel.GetDeclaredSymbol(modelClass, context.CancellationToken);
            if (modelType.GetMembers().OfType<IPropertySymbol>().Any(p => p.IsNavigationPropertyId(propertySymbol)))
            {
                return;
            }

            var action = CodeAction.Create("Create a foreign key property",
                t => Execute(context.Document, modelClass, navigationProperty, propertySymbol, idProperty, t));
            context.RegisterRefactoring(action);
        }

        private async Task<Document> Execute(Document document,
            ClassDeclarationSyntax @class,
            PropertyDeclarationSyntax navigationProperty,
            IPropertySymbol navigationPropertySymbol,
            IPropertySymbol idPropertySymbol,
            CancellationToken cancellationToken)
        {
            var editor = await DocumentEditor.CreateAsync(document, cancellationToken);

            var idSuffix = idPropertySymbol.Name.EndsWith("ID") ? "ID" : "Id";
            var idType = SF.ParseTypeName(idPropertySymbol.Type.ToMinimalDisplayString(editor.SemanticModel, navigationProperty.SpanStart));
            var propertyName = navigationPropertySymbol.Name + idSuffix;

            var finalProperty = CreateNavigationProperty(navigationProperty, idType, propertyName);
            editor.ReplaceNode(@class, @class.WithMembers(@class.Members.Insert(@class.Members.IndexOf(navigationProperty) + 1, finalProperty)));
            return editor.GetChangedDocument();
        }

        private static PropertyDeclarationSyntax CreateNavigationProperty(PropertyDeclarationSyntax navigationProperty, TypeSyntax typeSyntax, string propertyName)
        {
            return SF.PropertyDeclaration(typeSyntax, propertyName)
                .AddAccessorListAccessors(SF.AccessorDeclaration(SyntaxKind.GetAccessorDeclaration).WithSemicolonToken(SF.Token(SyntaxKind.SemicolonToken)),
                                          SF.AccessorDeclaration(SyntaxKind.SetAccessorDeclaration).WithSemicolonToken(SF.Token(SyntaxKind.SemicolonToken)))
                .WithModifiers(navigationProperty.Modifiers.Remove(SF.Token(SyntaxKind.VirtualKeyword)));
        }
    }
}
