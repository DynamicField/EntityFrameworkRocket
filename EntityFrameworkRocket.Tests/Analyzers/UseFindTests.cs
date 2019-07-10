using System.Collections.Generic;
using EntityFrameworkRocket.Analyzers;
using Gu.Roslyn.Asserts;
using NUnit.Framework;

namespace EntityFrameworkRocket.Tests.Analyzers
{
    public class UseFindTests : AnalyzerTest<UseFindAnalyzer>
    {
        private static string TestCode(string code) => CodeTemplates.LinqContext(code);
        private static IEnumerable<string> AllMethods => UseFindAnalyzer.CompatibleMethods;

        [TestCaseSource(nameof(AllMethods))]
        public void Analyzer_IdEqualsNumber_Reports(string method)
        {
            var code = TestCode($"↓context.Things.{method}(x => x.Id == 5);");
            RoslynAssert.Diagnostics(Analyzer, code);
        }

        [Test]
        public void Analyzer_NumberEqualsId_Reports()
        {
            var code = TestCode("↓context.Things.First(x => 5 == x.Id);");
            RoslynAssert.Diagnostics(Analyzer, code);
        }
        [Test]
        public void Analyzer_IdEqualsLambdaParameter_DoesNotReport()
        {
            var code = TestCode("↓context.Things.First(x => x.Id == x.Name.Length);");
            RoslynAssert.NoAnalyzerDiagnostics(Analyzer, code);
        }
        [Test]
        public void Analyzer_AsNoTrackingAndIdEqualsNumber_DoesNotReport()
        {
            var code = TestCode("↓context.Things.AsNoTracking().First(x => x.Id == 42);");
            RoslynAssert.NoAnalyzerDiagnostics(Analyzer, code);
        }

        [Test]
        public void Analyser_IdEqualsNumberMemberAccess_Reports()
        {
            var code = TestCode("var name = ↓context.Things.First(x => x.Id == 42).Name;");
            RoslynAssert.Diagnostics(Analyzer, code);
        }
    }
}