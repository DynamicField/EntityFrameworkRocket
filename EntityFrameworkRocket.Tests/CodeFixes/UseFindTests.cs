using EntityFrameworkRocket.Analyzers;
using EntityFrameworkRocket.CodeFixes;
using Gu.Roslyn.Asserts;
using NUnit.Framework;

namespace EntityFrameworkRocket.Tests.CodeFixes
{
    public class UseFindTests : CodeFixTest<UseFindCodeFix, UseFindAnalyzer>
    {
        private static string TestCode(string code) => CodeTemplates.LinqContext(code);

        [Test]
        public void CodeFix_FirstIdEqualsNumber_ReplacesWithFind()
        {
            var code = TestCode("↓context.Things.First(x => x.Id == 5);");
            var fixedCode = TestCode("context.Things.Find(5);");

            RoslynAssert.CodeFix(Analyzer, Fix, code, fixedCode);
        }
        [Test]
        public void CodeFix_FirstAsyncIdEqualsNumber_ReplacesWithFindAsync()
        {
            var code = TestCode("↓context.Things.FirstAsync(x => x.Id == 5);");
            var fixedCode = TestCode("context.Things.FindAsync(5);");

            RoslynAssert.CodeFix(Analyzer, Fix, code, fixedCode);
        }
    }
}