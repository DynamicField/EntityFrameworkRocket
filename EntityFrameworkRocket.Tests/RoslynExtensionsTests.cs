using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using NUnit.Framework;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;
namespace EntityFrameworkRocket.Tests
{
    [TestFixture]
    public class RoslynExtensionsTests
    {
        [Test]
        public void RemoveCall_Invocation_RemovesInvocation()
        {
            var node = InvocationExpression(IdentifierName("Test"));

            var result = node.RemoveCall();

            Assert.That(result, Is.EqualTo(node.Expression));
        }
        [Test]
        public void RemoveCall_MemberAccessInvocation_RemovesInvocationAndAccessor()
        {
            var node = InvocationExpression(MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression, IdentifierName("thing"), IdentifierName("Test")));

            var result = node.RemoveCall();

            var memberAccessExpression = ((MemberAccessExpressionSyntax)node.Expression).Expression;
            Assert.That(result, Is.EqualTo(memberAccessExpression));
        }

        [TestCase("Id")]
        [TestCase("ID")]
        [TestCase("TestId")]
        [TestCase("TestID")]
        public void IsId_IdProperty_IsValid(string propertyName)
        {
            var result = IsIdPropertyTest($"public int {propertyName} {{ get; set; }}");
            Assert.That(result, Is.True);
        }

        private static bool IsIdPropertyTest(string classProperty)
        {
            var (root, semanticModel) = TestCompilation.Create($@"
using Microsoft.EntityFrameworkCore;
class Test
{{
    {classProperty}
}}");
            var property = root.DescendantNodes().OfType<PropertyDeclarationSyntax>().FirstOrDefault();
            var symbol = semanticModel.GetDeclaredSymbol(property);
            var result = symbol.IsId();
            return result;
        }

        [Test]
        public void GetUnderlyingExpressionType_WithExpression_GivesUnderlyingType()
        {
            var (root, semanticModel) = TestCompilation.Create(@"
using System.Linq;
using System.Linq.Expressions;
Expression<Func<object>> variable;
");
            var variable = root.DescendantNodes().OfType<VariableDeclarationSyntax>().First();
            var expressionSymbol = (INamedTypeSymbol)semanticModel.GetTypeInfo(variable.Type).Type;
            var expectedUnderlyingType = expressionSymbol.TypeArguments[0];

            var result = expressionSymbol.GetUnderlyingExpressionType();

            Assert.That(result, Is.EqualTo(expectedUnderlyingType));
        }
        [Test]
        public void GetUnderlyingExpressionType_NotExpression_GivesSameType()
        {
            var (root, semanticModel) = TestCompilation.Create(@"
using System.Linq;
using System.Linq.Expressions;
object variable;
");
            var variable = root.DescendantNodes().OfType<VariableDeclarationSyntax>().First();
            var symbol = (INamedTypeSymbol)semanticModel.GetTypeInfo(variable.Type).Type;

            var result = symbol.GetUnderlyingExpressionType();

            Assert.That(result, Is.EqualTo(symbol));
        }

        [Test]
        public void IsNotMapped_WithNotMappedAttribute_ReturnsTrue()
        {
            var (root, semanticModel) = TestCompilation.Create(@"
using System.ComponentModel.DataAnnotations.Schema;
class Thing
{
    [NotMapped]
    public int DontMapMePlease { get; set; }
}
");
            var property = root.DescendantNodes().OfType<PropertyDeclarationSyntax>().First();
            var propertySymbol = semanticModel.GetDeclaredSymbol(property);

            var result = propertySymbol.IsNotMapped();

            Assert.That(result, Is.True);
        }
    }
}
