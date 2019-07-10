using System;
using EntityFrameworkRocket.Refactorings;
using Gu.Roslyn.Asserts;
using NUnit.Framework;
using static EntityFrameworkRocket.Refactorings.AddTrackingRefactoring;
namespace EntityFrameworkRocket.Tests.Refactorings
{
    [TestFixture]
    public class AddTrackingTests : RefactoringTest<AddTrackingRefactoring>
    {
        private static string TitleFor(string name) =>
            name == "AsNoTracking" ? AddAsNoTrackingTitle : AddAsTrackingTitle;
        private static string TestCode(string code) => CodeTemplates.LinqContext(code);

        [TestCase("AsNoTracking")]
        [TestCase("AsTracking")]
        public void Refactoring_WithoutTrackingStatements_AddsTracking(string tracking)
        {
            var code = TestCode("context.Things↓.ToList();");
            var fixedCode = TestCode($"context.Things.{tracking}().ToList();");
            RoslynAssert.Refactoring(Refactoring, code, fixedCode, TitleFor(tracking));
        }

        [TestCase("AsTracking", "AsNoTracking")]
        [TestCase("AsNoTracking", "AsTracking")]
        public void Refactoring_WithTrackingStatements_ReplacesLastInvocation(string otherTracking, string targetTracking)
        {
            var code = TestCode($"context.Things↓.{targetTracking}().{otherTracking}().ToList();");
            var fixedCode = TestCode($"context.Things.{targetTracking}().{targetTracking}().ToList();");
            RoslynAssert.Refactoring(Refactoring, code, fixedCode, TitleFor(targetTracking));
        }

        [TestCase("AsNoTracking")]
        [TestCase("AsTracking")]
        public void Refactoring_WithTrackingStatement_RefactorOnlyOppositeTracking(string originalTracking)
        {
            var code = TestCode($"context.Things↓.{originalTracking}().ToList();");
            RoslynAssert.NoRefactoring(Refactoring, code, TitleFor(originalTracking));
        }
    }
}