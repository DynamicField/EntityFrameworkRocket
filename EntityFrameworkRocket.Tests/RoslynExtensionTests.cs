using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using Gu.Roslyn.Asserts;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using NUnit.Framework;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;
namespace EntityFrameworkRocket.Tests
{
    [TestFixture]
    public class RoslynExtensionTests
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
    }
}
