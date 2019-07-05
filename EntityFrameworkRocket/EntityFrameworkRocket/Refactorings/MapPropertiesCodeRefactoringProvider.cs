using System.Collections;
using System.Collections.Generic;
using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeRefactorings;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Editing;
using SF = Microsoft.CodeAnalysis.CSharp.SyntaxFactory;
namespace EntityFrameworkRocket.Refactorings
{
    [ExportCodeRefactoringProvider(LanguageNames.CSharp, Name = nameof(MapPropertiesCodeRefactoringProvider)), Shared]
    public class MapPropertiesCodeRefactoringProvider : CodeRefactoringProvider
    {
        public sealed override async Task ComputeRefactoringsAsync(CodeRefactoringContext context)
        {
            var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);

            // Find the node at the selection.
            var node = root.FindNode(context.Span).FirstAncestorOrSelf<ObjectCreationExpressionSyntax>();
            var lambdas = node?.AncestorsAndSelf().OfType<LambdaExpressionSyntax>().ToList();
            // Only offer a refactoring if the selected node is a good node.
            if (node is null || !(node is ObjectCreationExpressionSyntax objectCreation) || !lambdas.Any())
            {
                return;
            }
            // Find all parameters from all lambda ancestors.
            var parameters = lambdas.SelectMany(l =>
            {
                switch (l)
                {
                    case SimpleLambdaExpressionSyntax s:
                        return new[] { s.Parameter };
                    case ParenthesizedLambdaExpressionSyntax p:
                        return p.ParameterList.Parameters;
                    default:
                        return Enumerable.Empty<ParameterSyntax>();
                }
            });
            var semanticModel = await context.Document.GetSemanticModelAsync(context.CancellationToken).ConfigureAwait(false);
            foreach (var parameter in parameters)
            {
                if (!(semanticModel.GetSymbolInfo(objectCreation.Type, context.CancellationToken).Symbol is ITypeSymbol newExpressionType)) continue;
                var lambdaParameterType = semanticModel.GetDeclaredSymbol(parameter, context.CancellationToken).Type;
                var properties = GetProperties(objectCreation, newExpressionType, lambdaParameterType).ToList();
                if (!properties.Any()) continue;
                // Create the action.
                CodeAction.Create($"Add mapping properties from \"{parameter.ToString()}\"",
                    c => Execute(context.Document, objectCreation, parameter, properties, c)).Register(context);
            }
        }

        private async Task<Document> Execute(Document document,
            ObjectCreationExpressionSyntax objectCreation,
            ParameterSyntax parameter,
            IEnumerable<IPropertySymbol> properties,
            CancellationToken cancellationToken)
        {
            var documentEditor = await DocumentEditor.CreateAsync(document, cancellationToken);
            var assignments = properties.Select(p => MakeAssignment(p, parameter)).Cast<ExpressionSyntax>().ToArray();
            documentEditor.ReplaceNode(objectCreation.Initializer, objectCreation.Initializer.AddExpressions(assignments));
            return documentEditor.GetChangedDocument(); // done!
        }

        private static IEnumerable<IPropertySymbol> GetProperties(ObjectCreationExpressionSyntax objectCreation,
            ITypeSymbol newExpressionType,
            ITypeSymbol lambdaParameterType)
        {
            var presentAssignments = objectCreation.Initializer.Expressions.OfType<AssignmentExpressionSyntax>()
                .Select(a => a.Left.ToString()).ToList();

            var newExpressionProperties = newExpressionType.GetMembers().OfType<IPropertySymbol>().Where(p => !p.IsReadOnly).ToList();
            var parameterProperties = lambdaParameterType.GetMembers().OfType<IPropertySymbol>().Where(p => !p.IsWriteOnly).ToList();
            var allProperties = newExpressionProperties.Where(x => parameterProperties.Any(p => p.Name == x.Name) && presentAssignments.All(n => n != x.Name)).ToList();
            return allProperties;
        }

        private static AssignmentExpressionSyntax MakeAssignment(IPropertySymbol property, ParameterSyntax parameter)
        {
            ExpressionSyntax propertyAccessor = SF.MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression, SF.IdentifierName(parameter.ToString()),
                SF.IdentifierName(property.Name));
            // In case of ICollection<>, or a type implementing ICollection<T>, append .ToList()
            bool IsCollection(INamedTypeSymbol t) => t.Name == nameof(ICollection) && t.TypeParameters.Length == 1;
            if (property.Type is INamedTypeSymbol type && (IsCollection(type) || type.AllInterfaces.Any(IsCollection)))
            {
                propertyAccessor = SF.InvocationExpression(SF.MemberAccessExpression(
                    SyntaxKind.SimpleMemberAccessExpression, propertyAccessor, SF.IdentifierName("ToList")));
            }
            return SF.AssignmentExpression(SyntaxKind.SimpleAssignmentExpression,
                SF.IdentifierName(property.Name),
                propertyAccessor);
            // Thing = x.Thing(.ToList())
        }
    }
}
