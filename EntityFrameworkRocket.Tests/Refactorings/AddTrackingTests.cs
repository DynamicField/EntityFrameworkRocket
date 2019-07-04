using System;
using EntityFrameworkRocket.Refactorings;
using Gu.Roslyn.Asserts;
using NUnit.Framework;

namespace EntityFrameworkRocket.Tests.Refactorings
{
    [TestFixture]
    public class AddTrackingTests : RefactoringTest<AddTrackingCodeRefactoringProvider>
    {
        private static string TestCode(string code) => CodeTemplates.LinqContext(code);

        private void DisableAsTracking(Action assertion)
        {
            Refactoring.DisableAsTracking = true;
            try
            {
                assertion();
            }
            finally
            {
                Refactoring.DisableAsTracking = false;
            }
        }
        private void DisableAsNoTracking(Action assertion)
        {
            Refactoring.DisableAsNoTracking = true;
            try
            {
                assertion();
            }
            finally
            {
                Refactoring.DisableAsNoTracking = false;
            }
        }
        [Test]
        public void AsNoTrackingRefactoring_WithNoTrackingStatements_AddsAsNoTracking()
        {
            var code = TestCode("context.Things.↓ToList();");
            var fixedCode = TestCode("context.Things.AsNoTracking().ToList();");
            DisableAsTracking(() => RoslynAssert.Refactoring(Refactoring, code, fixedCode));
        }
    }
}