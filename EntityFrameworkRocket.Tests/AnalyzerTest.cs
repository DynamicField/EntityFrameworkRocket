using Gu.Roslyn.Asserts;
using Microsoft.CodeAnalysis.Diagnostics;

namespace EntityFrameworkRocket.Tests
{
    public abstract class AnalyzerTest<TAnalyzer> where TAnalyzer : DiagnosticAnalyzer, new()
    {
        protected static readonly TAnalyzer Analyzer = new TAnalyzer();
    }
}