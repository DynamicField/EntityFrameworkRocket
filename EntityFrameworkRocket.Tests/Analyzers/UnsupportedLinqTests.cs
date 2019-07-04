using System;
using System.Linq;
using EntityFrameworkRocket.Analyzers;
using Gu.Roslyn.Asserts;
using Microsoft.CodeAnalysis;
using NUnit.Framework;
using NUnit.Framework.Internal;

namespace EntityFrameworkRocket.Tests.Analyzers
{
    [TestFixture]
    public class UnsupportedLinqTests : AnalyzerTest<UnsupportedLinqAnalyzer>
    {
        private static readonly ExpectedDiagnostic ExpectedDiagnostic = ExpectedDiagnostic.Create(UnsupportedLinqAnalyzer.Rule);

        private static string TestCode(string code) => CodeTemplates.LinqContext(code);

        [TestCase(nameof(Enumerable.Where))]
        [TestCase(nameof(Enumerable.Select))]
        [TestCase(nameof(Enumerable.TakeWhile))]
        [TestCase(nameof(Enumerable.SkipWhile))]
        public void Analyzer_DbSetWithIndexOverload_Reports(string methodName)
        {
            var code = TestCode($"context.Things.{methodName}((x, ↓i) => true).ToList();");
            RoslynAssert.Diagnostics(Analyzer, ExpectedDiagnostic, code);
        }

        [Test]
        public void Analyzer_DbSetWithSelectReturningInt_DoesNotReport()
        {
            var code = TestCode("context.Things.Select(x => x.Id).ToList();");
            RoslynAssert.Valid(Analyzer, code);
        }
        [Test]
        public void Analyzer_DbSetWithSelectReturningIntWithIndexOverload_Reports()
        {
            var code = TestCode("context.Things.Select((x, ↓i) => x.Id).ToList();");
            RoslynAssert.Diagnostics(Analyzer, ExpectedDiagnostic, code);
        }
        [Test]
        public void Analyzer_DbSetAsEnumerableWithIndexOverload_DoesNotReport()
        {
            var code = TestCode("context.Things.AsEnumerable().Select((x, i) => x.Id).ToList();");
            RoslynAssert.Valid(Analyzer, code);
        }
        [Test]
        public void Analyzer_IQueryableWithIndexOverload_DoesNotReport()
        {
            var code = TestCode("((IQueryable<Thing>)context.Things).Select((x, i) => x.Id).ToList();");
            RoslynAssert.Valid(Analyzer, code); 
        }
        [Test]
        public void Analyzer_IEnumerableWithIndexOverload_DoesNotReport()
        {
            var code = TestCode("Array.Empty<Thing>().Select((x, i) => x.Id).ToList();");
            RoslynAssert.Valid(Analyzer, code);
        }
    }
}
